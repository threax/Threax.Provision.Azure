using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DeployConfig;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppSecrets : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resourceConfiguration, AzureKeyVaultConfig azureKeyVaultConfig, DeploymentConfig deploymentConfig);
    }

    record CreateAppSecrets
    (
        ILogger<CreateAppSecrets> logger,
        IAppSecretCreator appSecretCreator,
        IKeyVaultManager keyVaultManager
    )
    : ICreateAppSecrets
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resourceConfiguration, AzureKeyVaultConfig azureKeyVaultConfig, DeploymentConfig deploymentConfig)
        {

            var idReg = resourceConfiguration.IdServerRegistration;
            if (idReg != null)
            {
                logger.LogInformation($"Configuring id server secrets for '{resourceConfiguration.Compute.Name}' in vault '{azureKeyVaultConfig.VaultName}' and in id server.");
                await keyVaultManager.UnlockSecrets(azureKeyVaultConfig.VaultName, config.UserId);
                switch (idReg.Type)
                {
                    case IdServerRegistrationType.None:
                        break;
                    case IdServerRegistrationType.AppDashboard:
                        await SetupAppDashboard(resourceConfiguration, azureKeyVaultConfig);
                        break;
                    case IdServerRegistrationType.RegularApp:
                        await SetupRegularApp(resourceConfiguration, azureKeyVaultConfig);
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(IdServerRegistrationType)} '{idReg.Type}' not supported.");
                }
            }
        }

        private async Task SetupAppDashboard(ResourceConfiguration resourceConfiguration, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            logger.LogInformation("Create jwt secret");
            var idReg = resourceConfiguration.IdServerRegistration;
            var jwtAuth = appSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);
        }

        private async Task SetupRegularApp(ResourceConfiguration resourceConfiguration, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            var idReg = resourceConfiguration.IdServerRegistration;

            logger.LogInformation("Create jwt secret");
            var jwtAuth = appSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);

            logger.LogInformation("Create client creds secret");
            var sharedClientCreds = appSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.ClientCredentialsSecretName, sharedClientCreds);
        }
    }
}
