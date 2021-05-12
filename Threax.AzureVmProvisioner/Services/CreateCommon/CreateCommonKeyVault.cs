using System.Threading.Tasks;
using Threax.Provision.AzPowershell;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.AzureVmProvisioner.Resources;

namespace Threax.AzureVmProvisioner.Controller.CreateCommon
{
    class CreateCommonKeyVault
    {
        private readonly IArmTemplateManager armTemplateManager;
        private readonly EnvironmentConfiguration config;
        private readonly IKeyVaultManager keyVaultManager;

        public CreateCommonKeyVault(IArmTemplateManager armTemplateManager, EnvironmentConfiguration config, IKeyVaultManager keyVaultManager)
        {
            this.armTemplateManager = armTemplateManager;
            this.config = config;
            this.keyVaultManager = keyVaultManager;
        }

        public async Task Execute()
        {
            if (!await keyVaultManager.Exists(config.InfraKeyVaultName))
            {
                var keyVaultArm = new ArmKeyVault(config.InfraKeyVaultName, config.Location, config.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, keyVaultArm);
            }

            //Allow AzDo user in the key vault if one is set.
            if (config.AzDoUser != null)
            {
                await keyVaultManager.UnlockSecretsRead(config.InfraKeyVaultName, config.AzDoUser.Value);
            }
        }
    }
}
