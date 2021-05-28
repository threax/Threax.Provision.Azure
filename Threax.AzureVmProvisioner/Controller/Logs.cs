
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DeployConfig;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ILogs : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Get the docker logs for the app.")]
    record Logs
    (
        ILogger<Logs> Logger,
        ISshCredsManager SshCredsManager
    ) : ILogs
    {
        public async Task Run(Configuration config)
        {
            var deploymentConfig = config.Deploy;

            Logger.LogInformation($"Getting logs for '{deploymentConfig.Name}'.");

            await SshCredsManager.RunSshCommand($"sudo docker logs {deploymentConfig.Name}");
        }
    }
}
