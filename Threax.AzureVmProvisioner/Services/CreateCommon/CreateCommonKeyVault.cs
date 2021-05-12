using System.Threading.Tasks;
using Threax.Provision.AzPowershell;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.AzureVmProvisioner.Resources;
using Microsoft.Extensions.Logging;

namespace Threax.AzureVmProvisioner.Controller.CreateCommon
{
    class CreateCommonKeyVault
    {
        private readonly IArmTemplateManager armTemplateManager;
        private readonly EnvironmentConfiguration config;
        private readonly IKeyVaultManager keyVaultManager;
        private readonly ILogger<CreateCommonKeyVault> logger;

        public CreateCommonKeyVault(
            IArmTemplateManager armTemplateManager,
            EnvironmentConfiguration config, 
            IKeyVaultManager keyVaultManager,
            ILogger<CreateCommonKeyVault> logger
            )
        {
            this.armTemplateManager = armTemplateManager;
            this.config = config;
            this.keyVaultManager = keyVaultManager;
            this.logger = logger;
        }

        public async Task Execute()
        {
            if (!await keyVaultManager.Exists(config.InfraKeyVaultName))
            {
                logger.LogInformation($"Creating infra key vault '{config.InfraKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(config.InfraKeyVaultName, config.Location, config.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, keyVaultArm);
            }

            //Allow AzDo user in the key vault if one is set.
            if (config.AzDoUser != null)
            {
                logger.LogInformation($"Adding AzDo user to infra key vault '{config.InfraKeyVaultName}'.");

                await keyVaultManager.UnlockSecretsRead(config.InfraKeyVaultName, config.AzDoUser.Value);
            }
        }
    }
}
