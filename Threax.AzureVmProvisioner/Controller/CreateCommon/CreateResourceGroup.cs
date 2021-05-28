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
    interface ICreateResourceGroup : IController
    {
        Task Run(EnvironmentConfiguration config);
    }

    [HelpInfo(HelpCategory.CreateCommon, "Create the app's resource group.")]
    record CreateResourceGroup
    (
        ILogger<CreateResourceGroup> logger,
        IArmTemplateManager armTemplateManager
    ) : ICreateResourceGroup
    {
        public async Task Run(EnvironmentConfiguration config)
        {
            //Resource Group
            logger.LogInformation($"Creating resource group '{config.ResourceGroup}'.");

            var armResourceGroup = new ArmResourceGroup(config.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(config.Location, armResourceGroup);
        }
    }
}
