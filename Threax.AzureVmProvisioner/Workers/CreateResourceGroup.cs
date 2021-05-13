using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.ResourceGroup;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateResourceGroup
    (
        ILogger<CreateResourceGroup> logger,
        EnvironmentConfiguration config,
        IArmTemplateManager armTemplateManager
    ) : IWorker<CreateResourceGroup>
    {
        public async Task ExecuteAsync()
        {
            //Resource Group
            logger.LogInformation($"Creating resource group '{config.ResourceGroup}'.");

            var armResourceGroup = new ArmResourceGroup(config.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(config.Location, armResourceGroup);
        }
    }
}
