using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Services
{
    class SshCredsManager : IDisposable, ISshCredsManager
    {
        private const string SshRuleName = "SSH";
        private const string LFPlaceholder = "**lf**";
        private readonly EnvironmentConfiguration config;
        private readonly IKeyVaultManager keyVaultManager;
        private readonly ICredentialLookup credentialLookup;
        private readonly IShellRunner shellRunner;
        private readonly IVmManager vmManager;
        private readonly IPathHelper appFolderFinder;
        private readonly ILogger<SshCredsManager> logger;
        private String publicKeyFile;
        private String privateKeyFile;
        private String sshKeyFolder;
        private String vmUser;
        private String sshHost;

        public SshCredsManager(EnvironmentConfiguration config,
            IKeyVaultManager keyVaultManager,
            ICredentialLookup credentialLookup,
            IShellRunner shellRunner,
            IVmManager vmManager,
            IPathHelper appFolderFinder,
            ILogger<SshCredsManager> logger)
        {
            this.config = config;
            this.keyVaultManager = keyVaultManager;
            this.credentialLookup = credentialLookup;
            this.shellRunner = shellRunner;
            this.vmManager = vmManager;
            this.appFolderFinder = appFolderFinder;
            this.logger = logger;
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(publicKeyFile))
            {
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
                    Task.Run(() => vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Deny")).GetAwaiter().GetResult();
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
                var outFile = Path.GetFullPath("newazurevm");
                var outPubFile = $"{outFile}.pub";
                try
                {
                    shellRunner.RunProcessVoid($"ssh-keygen -t rsa -b 4096 -o -a 100 -f {outFile}",
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

            await EnsureSshHost();

            var privateKeyPath = await LoadKeysAndGetSshPrivateKeyPath();
            var sshConnection = $"{vmUser}@{sshHost}";
            return await shellRunner.RunProcessGetExitAsync($"ssh -i {privateKeyPath} -t {sshConnection} {command}");
        }

        public async Task CopyStringToSshFile(string input, string dest)
        {
            var path = Path.GetTempFileName();
            try
            {
                await EnsureSshHost();
                File.WriteAllText(path, input);

                var privateKeyPath = await LoadKeysAndGetSshPrivateKeyPath();
                var finalDest = $"{vmUser}@{sshHost}:{dest}";
                await shellRunner.RunProcessVoidAsync($"scp -i {privateKeyPath} {path} {finalDest}",
                    invalidExitCodeMessage: $"Error running command scp for '{path}' to '{dest}'.");
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch(Exception ex)
                {
                    logger.LogError($"{ex.GetType().Name} deleting temp secret file '{path}'.");
                }
            }
        }

        public async Task CopySshFile(String file, String dest)
        {
            await EnsureSshHost();

            var privateKeyPath = await LoadKeysAndGetSshPrivateKeyPath();
            var finalDest = $"{vmUser}@{sshHost}:{dest}";
            await shellRunner.RunProcessVoidAsync($"scp -i {privateKeyPath} {file} {finalDest}",
                invalidExitCodeMessage: $"Error running command scp for '{file}' to '{dest}'.");
        }

        public async Task SaveSshKnownHostsSecret()
        {
            await EnsureSshHost();
            await vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Allow");
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

        private async Task<String> LoadKeysAndGetSshPrivateKeyPath()
        {
            if (privateKeyFile == null)
            {
                await vmManager.SetSecurityRuleAccess(config.NsgName, config.ResourceGroup, SshRuleName, "Allow");
                sshKeyFolder = Path.Combine(appFolderFinder.AppUserFolder, "sshkeys");
                if (!Directory.Exists(sshKeyFolder))
                {
                    Directory.CreateDirectory(sshKeyFolder);
                }

                publicKeyFile = Path.Combine(sshKeyFolder, "azure-ssh.pub");
                privateKeyFile = Path.Combine(sshKeyFolder, "azure-ssh");

                await EnsureSshHost();
                var knownHostsFile = Path.Combine(appFolderFinder.UserSshFolder, "known_hosts");
                if (!File.Exists(knownHostsFile))
                {
                    throw new InvalidOperationException($"Please create an ssh profile at '{knownHostsFile}'.");
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

                var creds = await credentialLookup.GetCredentials(config.InfraKeyVaultName, config.VmAdminBaseKey);
                vmUser = creds.User;

                //Validate that access has been granted
                int exitCode;
                int retry = 0;
                do
                {
                    logger.LogInformation($"Trying connection to '{sshHost}'. Retry '{retry}'.");
                    var sshConnection = $"{vmUser}@{sshHost}";

                    //Run a simple command to verify the connection.
                    exitCode = shellRunner.RunProcessGetExit($"ssh -i {privateKeyFile} -t {sshConnection} \"pwd\"");

                    if (++retry > 100)
                    {
                        throw new InvalidOperationException($"Could not establish connection to ssh host '{sshHost}' after '{retry}' retries. Giving up.");
                    }

                } while (exitCode != 0);
            }

            return privateKeyFile;
        }

        private async Task EnsureSshHost()
        {
            if (sshHost == null)
            {
                sshHost = await vmManager.GetPublicIp(config.PublicIpName);
            }
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
