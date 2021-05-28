using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.ResourceGroup;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateResourceGroup : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.CreateCommon, "Create the app's resource group.")]
    record CreateResourceGroup
    (
        ILogger<CreateResourceGroup> logger,
        IArmTemplateManager armTemplateManager
    ) : ICreateResourceGroup
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;

            //Resource Group
            logger.LogInformation($"Creating resource group '{envConfig.ResourceGroup}'.");

            var armResourceGroup = new ArmResourceGroup(envConfig.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(envConfig.Location, armResourceGroup);
        }
    }
}
