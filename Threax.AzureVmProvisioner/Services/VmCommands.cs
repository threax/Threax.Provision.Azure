using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Services
{
    class VmCommands : IVmCommands
    {
        private readonly IVmManager vmManager;
        private readonly ISshCredsManager sshCredsManager;
        private readonly IPathHelper pathHelper;

        public VmCommands(IVmManager vmManager, ISshCredsManager sshCredsManager, IPathHelper pathHelper)
        {
            this.vmManager = vmManager;
            this.sshCredsManager = sshCredsManager;
            this.pathHelper = pathHelper;
        }

        private String GetBasePath()
        {
            return Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Services");
        }

        public async Task ThreaxDockerToolsRun(String file, String content)
        {
            int exitCode;
            var tempFile = pathHelper.GetTempProvisionPath();
            try
            {
                File.WriteAllText(tempFile, content);

                var tempLoc = $"~/{Path.GetFileName(tempFile)}";
                await sshCredsManager.CopySshFile(tempFile, tempLoc);

                exitCode = await sshCredsManager.RunSshCommand($"sudo mkdir \"{Path.GetDirectoryName(file).Replace('\\', '/')}\"");
                exitCode = await sshCredsManager.RunSshCommand($"sudo mv \"{tempLoc}\" \"{file}\"");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Error moving the file.");
                }

                exitCode = await sshCredsManager.RunSshCommand($"sudo /app/.tools/Threax.DockerTools/bin/Threax.DockerTools run {file}");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Error during docker tools run.");
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        public async Task ThreaxDockerToolsExec(String file, String command, List<String> args)
        {
            var expanded = args.Count > 0 ? $"\"{String.Join("\" \"", args)}\"" : null; //TODO: Not secure
            var exitCode = await sshCredsManager.RunSshCommand($"sudo /app/.tools/Threax.DockerTools/bin/Threax.DockerTools \"exec\" \"{file}\" \"{command}\" {expanded}");
            if (exitCode != 0)
            {
                throw new InvalidOperationException("Error running exec.");
            }
        }

        public async Task RunSetupScript(String vmName, String resourceGroup, String acrHost, AcrCredential acrCreds)
        {
            var scriptName = "UbuntuSetup.sh";
            var scriptPath = Path.Combine(GetBasePath(), scriptName);
            var scriptDest = $"~/{scriptName}";
            await sshCredsManager.CopySshFile(scriptPath, scriptDest);
            var exitCode = await sshCredsManager.RunSshCommand($"chmod 777 \"{scriptDest}\"; sudo sh \"{scriptDest}\";rm \"{scriptDest}\";");
            if(exitCode != 0)
            {
                //This won't do happen, needs real error checking.
                throw new InvalidOperationException("Error running setup script.");
            }
            //Good way, send password as file
            var passwordFile = pathHelper.GetTempProvisionPath();
            try
            {
                File.WriteAllText(passwordFile, acrCreds.Password);

                var destPasswordFile = "~/acrpass";
                await sshCredsManager.CopySshFile(passwordFile, destPasswordFile);
                exitCode = await sshCredsManager.RunSshCommand($"cat \"{destPasswordFile}\" | sudo docker login -u \"{acrCreds.Username}\" --password-stdin \"{acrHost}\"; rm \"{destPasswordFile}\"");
                if (exitCode != 0)
                {
                    //This won't do happen, needs real error checking.
                    throw new InvalidOperationException($"Error Logging ACR '{acrHost}'.");
                }
            }
            finally
            {
                if (File.Exists(passwordFile))
                {
                    File.Delete(passwordFile);
                }
            }

            //Bad way that exposes password
            //exitCode = await sshCredsManager.RunSshCommand($"sudo docker login -u \"{acrCreds.Username}\" -p \"{acrCreds.Password}\" \"{acrHost}\"");
            //if (exitCode != 0)
            //{
            //    //This won't do happen, needs real error checking.
            //    throw new InvalidOperationException($"Error Logging ACR '{acrHost}'.");
            //}
        }

        public async Task SetSecretFromString(String vmName, String resourceGroup, String settingsFile, String settingsDest, String name, String content)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content);
                await SetSecretFromFile(vmName, resourceGroup, settingsFile, settingsDest, name, tempFile);
            }
            finally
            {
                //Any exceptions here are intentionally left to bubble up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        public async Task SetSecretFromFile(String vmName, String resourceGroup, String settingsFile, String settingsDest, String name, String source)
        {
            var tempPath = $"~/{Path.GetFileName(Path.GetRandomFileName())}";
            try
            {
                //Copy settings file
                var settingsFolder = Path.GetDirectoryName(settingsDest).Replace('\\', '/');
                await sshCredsManager.RunSshCommand($"sudo mkdir \"{settingsFolder}\"");
                await sshCredsManager.CopySshFile(settingsFile, tempPath);
                await sshCredsManager.RunSshCommand($"sudo mv \"{tempPath}\" \"{settingsDest}\"");

                //Copy Secret
                await sshCredsManager.CopySshFile(source, tempPath);
                var exitCode = await sshCredsManager.RunSshCommand($"sudo /app/.tools/Threax.DockerTools/bin/Threax.DockerTools SetSecret \"{settingsDest}\" \"{name}\" \"{tempPath}\"");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException("Error setting secret.");
                }
            }
            finally
            {
                await sshCredsManager.RunSshCommand($"sudo rm {tempPath}");
            }
        }

        private static string Escape(string content)
        {
            var sb = new StringBuilder(content.Length * 2);
            for(var i = 0; i < content.Length; ++i)
            {
                var c = content[i];
                var next = i + 1;
                bool isCrLf = c == '\r' && next < content.Length && content[next] == '\n';
                if (!isCrLf)
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
            }
            content = sb.ToString();
            return content;
        }
    }
}
