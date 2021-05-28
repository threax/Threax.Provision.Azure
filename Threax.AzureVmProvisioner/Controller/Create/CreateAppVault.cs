using System;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppVault : IController
    {
        Task Run(EnvironmentConfiguration config, AzureKeyVaultConfig azureKeyVaultConfig);
    }

    [HelpInfo(HelpCategory.Create, "Create the key vault for the given app.")]
    record CreateAppVault
    (
        IArmTemplateManager armTemplateManager,
        IKeyVaultManager keyVaultManager
    )
    : ICreateAppVault
    {
        public async Task Run(EnvironmentConfiguration config, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            if (!String.IsNullOrEmpty(azureKeyVaultConfig.VaultName))
            {
                if (!await keyVaultManager.Exists(azureKeyVaultConfig.VaultName))
                {
                    var keyVaultArm = new ArmKeyVault(azureKeyVaultConfig.VaultName, config.Location, config.TenantId.ToString());
                    await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, keyVaultArm);
                }

                //Allow AzDo user in the key vault if one is set.
                if (config.AzDoUser != null)
                {
                    await keyVaultManager.UnlockSecretsRead(azureKeyVaultConfig.VaultName, config.AzDoUser.Value);
                }
            }
        }
    }
}
