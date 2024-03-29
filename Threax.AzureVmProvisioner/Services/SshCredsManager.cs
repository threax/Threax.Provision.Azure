﻿using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Services
{
    interface ISshCredsManager
    {
        string PrivateKeySecretName { get; }
        string PublicKeySecretName { get; }

        Task CopySshFile(string file, string dest);
        Task CopyStringToSshFile(string input, string dest);
        void Dispose();
        Task<string> LoadPublicKey();
        Task<int> RunSshCommand(string command);
        Task OpenSshShell();
        Task SaveSshKnownHostsSecret();
    }

    class SshCredsManager : IDisposable, ISshCredsManager
    {
        record SshState(String publicKeyFile, String privateKeyFile, String vmUser) { }

        private const string SshRuleName = "SSH";
        private const string LFPlaceholder = "**lf**";
        private readonly EnvironmentConfiguration config;
        private readonly IKeyVaultManager keyVaultManager;
        private readonly ICredentialLookup credentialLookup;
        private readonly IShellRunner shellRunner;
        private readonly IVmManager vmManager;
        private readonly IPathHelper appFolderFinder;
        private readonly ILogger<SshCredsManager> logger;
        private readonly IOSHandler osHandler;
        private readonly IMachineIpManager machineIpManager;
        private readonly Lazy<Task<String>> sshHostLookup;
        private readonly Lazy<Task<SshState>> sshStateLoad;

        public SshCredsManager(EnvironmentConfiguration config,
            IKeyVaultManager keyVaultManager,
            ICredentialLookup credentialLookup,
            IShellRunner shellRunner,
            IVmManager vmManager,
            IPathHelper appFolderFinder,
            ILogger<SshCredsManager> logger,
            IOSHandler osHandler,
            IMachineIpManager machineIpManager)
        {
            this.config = config;
            this.keyVaultManager = keyVaultManager;
            this.credentialLookup = credentialLookup;
            this.shellRunner = shellRunner;
            this.vmManager = vmManager;
            this.appFolderFinder = appFolderFinder;
            this.logger = logger;
            this.osHandler = osHandler;
            this.machineIpManager = machineIpManager;
            sshHostLookup = new Lazy<Task<string>>(() => vmManager.GetPublicIp(config.PublicIpName));
            sshStateLoad = new Lazy<Task<SshState>>(() => LoadSshState());
        }

        public void Dispose()
        {
            if (sshStateLoad.IsValueCreated)
            {
                var state = sshStateLoad.Value.GetAwaiter().GetResult();
                var publicKeyFile = state.publicKeyFile;
                var privateKeyFile = state.privateKeyFile;

                try
                {
                    File.Delete(publicKeyFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An {ex.GetType().Name} occured cleaning up public key file '{publicKeyFile}'. Message: {ex.Message}\n{ex.StackTrace}");
                }
                try
                {
                    File.Delete(privateKeyFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An {ex.GetType().Name} occured cleaning up private key file '{privateKeyFile}'. Message: {ex.Message}\n{ex.StackTrace}");
                }
                try
                {
                    Task.Run(() => vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Deny", "*")).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An {ex.GetType().Name} occured disabling access to ssh in '{config.NsgName}' rg: '{config.ResourceGroup}' Rule Name: '{SshRuleName}'. Message: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public async Task<String> LoadPublicKey()
        {
            var publicKeyName = PublicKeySecretName;
            var publicKey = UnpackKey(await keyVaultManager.GetSecret(config.InfraKeyVaultName, publicKeyName));
            if (publicKey == null)
            {
                logger.LogInformation("Need to create ssh keypair. Please press enter to the prompts below.");
                var outFile = Path.GetTempFileName();
                var outPubFile = $"{outFile}.pub";
                File.Delete(outFile);
                try
                {
                    //Can't get -N '' to work so just send 2 newlines when making this
                    var newlines = $"{Environment.NewLine}{Environment.NewLine}";
                    shellRunner.RunProcessVoid($"cat {newlines} | ssh-keygen -t rsa -b 4096 -o -a 100 -f {outFile}",
                       invalidExitCodeMessage: $"Error creating keys with ssh-keygen.");

                    var privateKey = File.ReadAllText(outFile);
                    //Clean up newlines in private key, this should work on any os
                    privateKey = PackKey(privateKey);
                    await keyVaultManager.SetSecret(config.InfraKeyVaultName, PrivateKeySecretName, privateKey);

                    //Set public key last since this is what satisfies the condition above, don't want to be in a half state.
                    publicKey = File.ReadAllText(outPubFile);
                    publicKey = PackKey(publicKey); //Pack the key to store it
                    if (publicKey.EndsWith(LFPlaceholder))
                    {
                        publicKey = publicKey.Substring(0, publicKey.Length - LFPlaceholder.Length);
                    }
                    await keyVaultManager.SetSecret(config.InfraKeyVaultName, publicKeyName, publicKey);

                    publicKey = UnpackKey(publicKey); //Unpack the key again to return it
                }
                finally
                {
                    if (File.Exists(outFile))
                    {
                        try
                        {
                            File.Delete(outFile);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"{ex.GetType().Name} erasing file {outFile}. Please ensure this file is erased manually.");
                        }
                    }
                    if (File.Exists(outPubFile))
                    {
                        try
                        {
                            File.Delete(outPubFile);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"{ex.GetType().Name} erasing file {outFile}. Please ensure this file is erased manually.");
                        }
                    }

                    //throw new InvalidOperationException($"You must create a key pair with \"ssh-keygen -t rsa -b 4096 -o -a 100 -f newazurevm\" and save it as '{publicKeyName}' and '{PrivateKeySecretName}' in the '{config.InfraKeyVaultName}' key vault. Also replace all the newlines with '**lf**'. This is needed to preserve them when they are reloaded. Then run this program again. There is no automation for this step at this time.");
                }

                logger.LogInformation("Finished creating new ssh key. Continuing.");
            }

            return publicKey;
        }

        public async Task<int> RunSshCommand(String command)
        {
            //Commands run here are not escaped, TODO: Escape ssh commands

            var sshHost = await sshHostLookup.Value;
            var sshState = await sshStateLoad.Value;
            var sshConnection = $"{sshState.vmUser}@{sshHost}";
            return await shellRunner.RunProcessGetExitAsync($"ssh -i {sshState.privateKeyFile} -t {sshConnection} {command}");
        }

        public async Task OpenSshShell()
        {
            var sshHost = await sshHostLookup.Value;
            var sshState = await sshStateLoad.Value;
            var sshConnection = $"{sshState.vmUser}@{sshHost}";
            var startInfo = new ProcessStartInfo("ssh") { ArgumentList = { "-i", sshState.privateKeyFile, "-t", sshConnection } };
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = false;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = false;
            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        public async Task CopyStringToSshFile(string input, string dest)
        {
            var path = Path.GetTempFileName();
            try
            {
                var sshHost = await sshHostLookup.Value;
                File.WriteAllText(path, input);

                var sshState = await sshStateLoad.Value;
                var finalDest = $"{sshState.vmUser}@{sshHost}:{dest}";
                await shellRunner.RunProcessVoidAsync($"scp -i {sshState.privateKeyFile} {path} {finalDest}",
                    invalidExitCodeMessage: $"Error running command scp for '{path}' to '{dest}'.");
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    logger.LogError($"{ex.GetType().Name} deleting temp secret file '{path}'.");
                }
            }
        }

        public async Task CopySshFile(String file, String dest)
        {
            var sshHost = await sshHostLookup.Value;
            var sshState = await sshStateLoad.Value;
            var finalDest = $"{sshState.vmUser}@{sshHost}:{dest}";
            await shellRunner.RunProcessVoidAsync($"scp -i {sshState.privateKeyFile} {file} {finalDest}",
                invalidExitCodeMessage: $"Error running command scp for '{file}' to '{dest}'.");
        }

        public async Task SaveSshKnownHostsSecret()
        {
            var sshHost = await sshHostLookup.Value;
            var ip = await machineIpManager.GetExternalIp();
            await vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Allow", ip);
            String key;
            int retry = 0;
            do
            {
                logger.LogInformation($"Trying key scan connection to '{sshHost}'. Retry '{retry}'.");
                var builder = shellRunner.CreateCommandBuilder();
                builder.AddCommand($"$key = ssh-keyscan -t rsa {sshHost}");
                builder.AddResultCommand($"$key | ConvertTo-Json -Depth 10");
                key = shellRunner.RunProcess<String>(builder);

                if (++retry > 100)
                {
                    throw new InvalidOperationException($"Retried ssh-keyscan '{retry}' times. Giving up.");
                }
            } while (String.IsNullOrEmpty(key));

            var existing = await keyVaultManager.GetSecret(config.InfraKeyVaultName, config.SshKnownHostKey);
            if (existing != null && existing != key)
            {
                logger.LogInformation($"Current saved server key (top) does not match current key on server (bottom). \n'{existing}'\n{key}");
                logger.LogInformation("If this is because the vm was recreated please enter y below. Otherwise this will be considered an error and the provisioning will stop.");

                if (!"y".Equals(Console.ReadLine(), StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException("The ssh keys did not match and were rejected by the user. Key vault not updated.");
                }
            }

            await keyVaultManager.SetSecret(config.InfraKeyVaultName, config.SshKnownHostKey, key);
        }

        public String PublicKeySecretName => $"{config.VmAdminBaseKey}-ssh-public-key";

        public String PrivateKeySecretName => $"{config.VmAdminBaseKey}-ssh-private-key";

        private async Task<SshState> LoadSshState()
        {
            var ip = await machineIpManager.GetExternalIp();
            await vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Allow", ip);
            var sshKeyFolder = Path.Combine(appFolderFinder.AppUserFolder, "sshkeys");
            if (!Directory.Exists(sshKeyFolder))
            {
                Directory.CreateDirectory(sshKeyFolder);
            }

            var publicKeyFile = Path.Combine(sshKeyFolder, "azure-ssh.pub");
            var privateKeyFile = Path.Combine(sshKeyFolder, "azure-ssh");

            var sshHost = await sshHostLookup.Value;
            var knownHostsFile = Path.Combine(appFolderFinder.UserSshFolder, "known_hosts");
            if (!File.Exists(knownHostsFile))
            {
                var knownHostsPath = Path.GetDirectoryName(knownHostsFile);
                if (!Directory.Exists(knownHostsPath))
                {
                    Directory.CreateDirectory(knownHostsPath);
                }
                using var stream = File.Create(knownHostsFile);
                //throw new InvalidOperationException($"Please create an ssh profile at '{knownHostsFile}'.");
            }

            var key = await keyVaultManager.GetSecret(config.InfraKeyVaultName, config.SshKnownHostKey);
            var currentKeys = File.ReadAllText(knownHostsFile);
            if (!currentKeys.Contains(key))
            {
                File.AppendAllText(knownHostsFile, key);
            }

            var publicKey = UnpackKey(await keyVaultManager.GetSecret(config.InfraKeyVaultName, PublicKeySecretName));
            var privateKey = UnpackKey(await keyVaultManager.GetSecret(config.InfraKeyVaultName, PrivateKeySecretName));

            File.WriteAllText(publicKeyFile, publicKey);
            File.WriteAllText(privateKeyFile, privateKey);

            var user = osHandler.GetUser();
            var group = osHandler.GetGroup();
            osHandler.SetPermissions(publicKeyFile, user, group);
            osHandler.SetPermissions(privateKeyFile, user, group);

            var creds = await credentialLookup.GetCredentials(config.InfraKeyVaultName, config.VmAdminBaseKey);
            var vmUser = creds.User;

            //Validate that access has been granted
            int exitCode;
            int retry = 0;
            do
            {
                logger.LogInformation($"Trying connection to '{sshHost}'. Retry '{retry}'.");
                var sshConnection = $"{vmUser}@{sshHost}";

                //Run a simple command to verify the connection.
                exitCode = await shellRunner.RunProcessGetExitAsync($"ssh -i {privateKeyFile} -t {sshConnection} \"pwd\"");

                if (++retry > 100)
                {
                    throw new InvalidOperationException($"Could not establish connection to ssh host '{sshHost}' after '{retry}' retries. Giving up.");
                }

            } while (exitCode != 0);

            return new SshState(publicKeyFile, privateKeyFile, vmUser);
        }

        private static string PackKey(string key)
        {
            key = key?.Replace("\r", "")?.Replace("\n", LFPlaceholder);
            return key;
        }

        private static string UnpackKey(string key)
        {
            key = key?.Replace(LFPlaceholder, "\n");
            return key;
        }
    }
}
