using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateAppVault
    (
        IArmTemplateManager armTemplateManager,
        EnvironmentConfiguration config,
        IKeyVaultManager keyVaultManager,
        AzureKeyVaultConfig azureKeyVaultConfig
    )
    : IWorker<CreateAppVault>
    {
        public async Task ExecuteAsync()
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
