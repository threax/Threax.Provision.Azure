using System;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppVault : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Create, "Create the key vault for the given app.")]
    record CreateAppVault
    (
        IArmTemplateManager armTemplateManager,
        IKeyVaultManager keyVaultManager
    )
    : ICreateAppVault
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var azureKeyVaultConfig = config.KeyVault;

            if (!String.IsNullOrEmpty(azureKeyVaultConfig.VaultName))
            {
                if (!await keyVaultManager.Exists(azureKeyVaultConfig.VaultName))
                {
                    var keyVaultArm = new ArmKeyVault(azureKeyVaultConfig.VaultName, envConfig.Location, envConfig.TenantId.ToString());
                    await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, keyVaultArm);
                }

                //Allow AzDo user in the key vault if one is set.
                if (envConfig.AzDoUser != null)
                {
                    await keyVaultManager.UnlockSecrets(azureKeyVaultConfig.VaultName, envConfig.AzDoUser.Value);
                }
            }
        }
    }
}
