using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Threax.ProcessHelper;
using Threax.ProcessHelper.Pwsh;

namespace Threax.AzureVmProvisioner.Services
{
    public class OSHandlerUnix : IOSHandler
    {
        private readonly IProcessRunner processRunner;
        private readonly IPowershellCoreRunner powershellCoreRunner;

        public OSHandlerUnix(IProcessRunner processRunner, IPowershellCoreRunner powershellCoreRunner)
        {
            this.processRunner = processRunner;
            this.powershellCoreRunner = powershellCoreRunner;
        }

        public string CreateDockerPath(string path)
        {
            return path;
        }

        public string GetUser()
        {
            var user = powershellCoreRunner.RunProcess<String>($"id -un | ConvertTo-Json -Depth 1");
            return user;
        }

        public string GetGroup()
        {
            var group = powershellCoreRunner.RunProcess<String>($"id -gn | ConvertTo-Json -Depth 1");
            return group;
        }

        public void SetPermissions(string path, string user, string group)
        {
            //sudo chown -R 19999:19999 /data/app/id
            //sudo chmod 700 /data/app/id

            //This is needed on linux, but for now the permissions are breaking stuff

            int exitCode;
            exitCode = this.processRunner.Run(new System.Diagnostics.ProcessStartInfo("chown") { ArgumentList = {"-R", $"{user}:{group}", path } });
            if (exitCode != 0)
            {
                throw new InvalidOperationException("An error occured during the chown.");
            }
            exitCode = this.processRunner.Run(new System.Diagnostics.ProcessStartInfo("chmod") { ArgumentList = { "-R", "700", path } });
            if (exitCode != 0)
            {
                throw new InvalidOperationException("An error occured during the chmod.");
            }
        }

        public void MakeExecutable(string path)
        {
            var exitCode = this.processRunner.Run(new System.Diagnostics.ProcessStartInfo("chmod") { ArgumentList = { "+x", path } });
            if (exitCode != 0)
            {
                throw new InvalidOperationException("An error occured during the chmod.");
            }
        }
    }
}
