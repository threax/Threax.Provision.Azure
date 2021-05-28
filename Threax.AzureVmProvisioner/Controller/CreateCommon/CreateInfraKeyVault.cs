﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    interface ICreateInfraKeyVault : IController
    {
        Task Run(EnvironmentConfiguration config);
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
        public async Task Run(EnvironmentConfiguration config)
        {
            //Key Vaults
            logger.LogInformation($"Setting up infra key vault '{config.InfraKeyVaultName}'.");

            if (!await keyVaultManager.Exists(config.InfraKeyVaultName))
            {
                logger.LogInformation($"Creating infra key vault '{config.InfraKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(config.InfraKeyVaultName, config.Location, config.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, keyVaultArm);
            }

            logger.LogInformation($"Setting up external key vault '{config.ExternalKeyVaultName}'.");

            if (!await keyVaultManager.Exists(config.ExternalKeyVaultName))
            {
                logger.LogInformation($"Creating external key vault '{config.ExternalKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(config.ExternalKeyVaultName, config.Location, config.TenantId.ToString());
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