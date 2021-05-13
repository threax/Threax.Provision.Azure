using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateInfraKeyVault
    (
        ILogger<CreateInfraKeyVault> logger,
        EnvironmentConfiguration config,
        IArmTemplateManager armTemplateManager,
        IKeyVaultManager keyVaultManager,
        IKeyVaultAccessManager keyVaultAccessManager
    )
     : IWorker<CreateInfraKeyVault>
    {
        public async Task ExecuteAsync()
        {
            //Key Vaults
            logger.LogInformation($"Setting up infra key vault '{config.InfraKeyVaultName}'.");

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
