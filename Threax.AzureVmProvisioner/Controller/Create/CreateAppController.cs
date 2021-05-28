using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppController : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Create, "Create the app's docker info on the compute instance.")]
    record CreateAppController
    (
        IKeyVaultManager keyVaultManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ILogger<CreateAppController> logger,
        IAppInsightsManager appInsightsManager,
        IServicePrincipalManager servicePrincipalManager,
        IVmCommands vmCommands,
        IPathHelper pathHelper
    ) : ICreateAppController
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var resources = config.Resources;
            var azureKeyVaultConfig = config.KeyVault;

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

                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, envConfig.UserId);

                if (!await servicePrincipalManager.Exists(spName))
                {
                    await CreateServicePrincipal(spName, envConfig, azureKeyVaultConfig);
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

                    await CreateServicePrincipal(spName, envConfig, azureKeyVaultConfig);

                    id = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-id");
                }
                await keyVaultManager.UnlockSecretsRead(azureKeyVaultConfig.VaultName, Guid.Parse(id));

                //Setup App Connection String Secret
                logger.LogInformation("Setting app key vault connection string secret.");
                var vaultCs = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, "sp-connectionstring");

                var fileName = Path.GetFileName(config.GetConfigPath());
                var serverConfigFilePath = $"/app/{resource.Name}/{fileName}";

                await vmCommands.SetSecretFromString(envConfig.VmName, envConfig.ResourceGroup, config.GetConfigPath(), serverConfigFilePath, "serviceprincipal-cs", vaultCs);
            }

            //Setup App Insights
            if (!String.IsNullOrEmpty(resource.AppInsightsSecretName))
            {
                logger.LogInformation($"Setting instrumentation key secret for App Insights '{envConfig.AppInsightsName}' in Resource Group '{envConfig.ResourceGroup}'");

                var instrumentationKey = await appInsightsManager.GetAppInsightsInstrumentationKey(envConfig.AppInsightsName, envConfig.ResourceGroup);
                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, envConfig.UserId);
                await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, resource.AppInsightsSecretName, instrumentationKey);
            }
        }

        private async Task CreateServicePrincipal(string spName, EnvironmentConfiguration config, AzureKeyVaultConfig azureKeyVaultConfig)
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
