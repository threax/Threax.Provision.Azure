using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateAppSecrets
    (
        EnvironmentConfiguration config,
        ResourceConfiguration resourceConfiguration,
        ILogger<CreateAppSecrets> logger,
        IAppSecretCreator AppSecretCreator,
        IKeyVaultManager keyVaultManager,
        AzureKeyVaultConfig azureKeyVaultConfig,
        DeploymentConfig DeploymentConfig
    )
    : IWorker<CreateAppSecrets>
    {
        public async Task ExecuteAsync()
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
                        await SetupAppDashboard();
                        break;
                    case IdServerRegistrationType.RegularApp:
                        await SetupRegularApp();
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(IdServerRegistrationType)} '{idReg.Type}' not supported.");
                }
            }
        }

        private async Task SetupAppDashboard()
        {
            logger.LogInformation("Create jwt secret");
            var idReg = resourceConfiguration.IdServerRegistration;
            var jwtAuth = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);
        }

        private async Task SetupRegularApp()
        {
            var idReg = resourceConfiguration.IdServerRegistration;

            logger.LogInformation("Create jwt secret");
            var jwtAuth = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);

            logger.LogInformation("Create client creds secret");
            var sharedClientCreds = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.ClientCredentialsSecretName, sharedClientCreds);
        }
    }
}
