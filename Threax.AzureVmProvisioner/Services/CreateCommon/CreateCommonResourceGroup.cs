using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.ResourceGroup;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Services.CreateCommon
{
    class CreateCommonResourceGroup
    {
        private readonly IArmTemplateManager armTemplateManager;
        private readonly EnvironmentConfiguration config;
        private readonly ILogger<CreateCommonResourceGroup> logger;

        public CreateCommonResourceGroup(
            IArmTemplateManager armTemplateManager, 
            EnvironmentConfiguration config,
            ILogger<CreateCommonResourceGroup> logger)
        {
            this.armTemplateManager = armTemplateManager;
            this.config = config;
            this.logger = logger;
        }

        public async Task Execute()
        {
            logger.LogInformation($"Creating resource group '{config.ResourceGroup}'.");

            var armResourceGroup = new ArmResourceGroup(config.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(config.Location, armResourceGroup);
        }
    }
}
