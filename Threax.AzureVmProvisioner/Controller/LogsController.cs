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
    interface ILogsController : IController
    {
        Task Run(DeploymentConfig DeploymentConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Get the docker logs for the app.")]
    record LogsController
    (
        ILogger<LogsController> Logger,
        ISshCredsManager SshCredsManager
    ) : ILogsController
    {
        public async Task Run(DeploymentConfig DeploymentConfig)
        {
            Logger.LogInformation($"Getting logs for '{DeploymentConfig.Name}'.");

            await SshCredsManager.RunSshCommand($"sudo docker logs {DeploymentConfig.Name}");
        }
    }
}
