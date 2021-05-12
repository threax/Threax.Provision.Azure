using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.ResourceGroup;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller.CreateCommon
{
    class CreateCommonResourceGroup
    {
        private readonly IArmTemplateManager armTemplateManager;
        private readonly EnvironmentConfiguration config;

        public CreateCommonResourceGroup(IArmTemplateManager armTemplateManager, EnvironmentConfiguration config)
        {
            this.armTemplateManager = armTemplateManager;
            this.config = config;
        }

        public async Task Execute()
        {
            var armResourceGroup = new ArmResourceGroup(config.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(config.Location, armResourceGroup);
        }
    }
}
