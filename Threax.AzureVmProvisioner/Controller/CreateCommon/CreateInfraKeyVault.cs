using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateInfraKeyVault : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.CreateCommon, "Create the common infrastructure key vault.")]
    record CreateInfraKeyVault
    (
        ILogger<CreateInfraKeyVault> logger,
        IArmTemplateManager armTemplateManager,
        IKeyVaultManager keyVaultManager,
        IKeyVaultAccessManager keyVaultAccessManager
    )
     : ICreateInfraKeyVault
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            //Key Vaults
            logger.LogInformation($"Setting up infra key vault '{envConfig.InfraKeyVaultName}'.");

            if (!await keyVaultManager.Exists(envConfig.InfraKeyVaultName))
            {
                logger.LogInformation($"Creating infra key vault '{envConfig.InfraKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(envConfig.InfraKeyVaultName, envConfig.Location, envConfig.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, keyVaultArm);
            }

            logger.LogInformation($"Setting up external key vault '{envConfig.ExternalKeyVaultName}'.");

            if (!await keyVaultManager.Exists(envConfig.ExternalKeyVaultName))
            {
                logger.LogInformation($"Creating external key vault '{envConfig.ExternalKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(envConfig.ExternalKeyVaultName, envConfig.Location, envConfig.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, keyVaultArm);
            }

            //Allow AzDo user in the key vault if one is set.
            if (envConfig.AzDoUser != null)
            {
                logger.LogInformation($"Adding AzDo user to infra key vault '{envConfig.InfraKeyVaultName}'.");

                await keyVaultManager.UnlockSecretsRead(envConfig.InfraKeyVaultName, envConfig.AzDoUser.Value);
            }
        }
    }
}
