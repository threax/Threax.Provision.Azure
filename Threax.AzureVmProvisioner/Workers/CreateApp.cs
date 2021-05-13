using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateApp
    (
        EnvironmentConfiguration config,
        ResourceConfiguration resources,
        AzureKeyVaultConfig azureKeyVaultConfig,
        IKeyVaultManager keyVaultManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ILogger<CreateApp> logger,
        IAppInsightsManager appInsightsManager,
        IServicePrincipalManager servicePrincipalManager,
        IVmCommands vmCommands,
        IPathHelper pathHelper
    )
    : IWorker<CreateApp>
    {
        public async Task ExecuteAsync()
        {
            var resource = resources.Compute;

            if (resource == null)
            {
                return;
            }

            logger.LogInformation($"Processing app compute for '{resource.Name}'");

            //Update app permissions in key vault
            if (!string.IsNullOrEmpty(azureKeyVaultConfig.VaultName))
            {
                var spName = $"{resource.Name}-app";
                logger.LogInformation($"Managing service principal '{spName}'.");

                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, config.UserId);

                if (!await servicePrincipalManager.Exists(spName))
                {
                    await CreateServicePrincipal(spName);
                }

                var id = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-id");
                if (id == null)
                {
                    //If this id is null the service principal id was lost for this app. Recreate it
                    logger.LogInformation($"Cannot find service principal id for '{spName}'. Recreating it.");

                    if (await servicePrincipalManager.Exists(spName))
                    {
                        await servicePrincipalManager.Remove(spName);
                    }

                    await CreateServicePrincipal(spName);

                    id = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-id");
                }
                await keyVaultManager.UnlockSecretsRead(azureKeyVaultConfig.VaultName, Guid.Parse(id));

                //Setup App Connection String Secret
                logger.LogInformation("Setting app key vault connection string secret.");
                var vaultCs = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-connectionstring");

                var fileName = Path.GetFileName(pathHelper.ConfigPath);
                var serverConfigFilePath = $"/app/{resource.Name}/{fileName}";

                await vmCommands.SetSecretFromString(config.VmName, config.ResourceGroup, pathHelper.ConfigPath, serverConfigFilePath, "serviceprincipal-cs", vaultCs);
            }

            //Setup App Insights
            if (!String.IsNullOrEmpty(resource.AppInsightsSecretName))
            {
                logger.LogInformation($"Setting instrumentation key secret for App Insights '{config.AppInsightsName}' in Resource Group '{config.ResourceGroup}'");

                var instrumentationKey = await appInsightsManager.GetAppInsightsInstrumentationKey(config.AppInsightsName, config.ResourceGroup);
                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, config.UserId);
                await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, resource.AppInsightsSecretName, instrumentationKey);
            }
        }

        private async Task CreateServicePrincipal(string spName)
        {
            logger.LogInformation($"Creating service principal '{spName}'.");

            var sp = await servicePrincipalManager.CreateServicePrincipal(spName, config.SubscriptionId, config.ResourceGroup);
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, "sp-id", sp.Id);
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, "sp-appkey", sp.Secret);
            var appKey = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-appkey");
            var spConnectionString = $"RunAs=App;AppId={sp.ApplicationId};TenantId={config.TenantId};AppKey={appKey}";
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, "sp-connectionstring", spConnectionString);
        }
    }
}
