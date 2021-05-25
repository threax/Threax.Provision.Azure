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
    record LogsController
    (
        EnvironmentConfiguration EnvironmentConfiguration,
        ResourceConfiguration ResourceConfiguration,
        DeploymentConfig DeploymentConfig,
        ILogger<LogsController> Logger,
        ISshCredsManager SshCredsManager
    ) : IController
    {
        public async Task Run()
        {
            Logger.LogInformation($"Getting logs for '{DeploymentConfig.Name}'.");

            await SshCredsManager.RunSshCommand($"sudo docker logs {DeploymentConfig.Name}");
        }
    }
}
