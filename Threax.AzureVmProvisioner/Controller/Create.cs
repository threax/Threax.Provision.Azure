using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreate : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Create all the resources for an individual app.")]
    record Create
    (
        ILogger<Create> logger,
        IRunInfoLogger runInfoLogger,
        ICreateAppCertificate CreateAppCertificate,
        ICreateAppController CreateApp,
        ICreateAppVault CreateAppVault,
        ICreateAppSqlDatabase CreateAppSqlDatabase,
        ICreateAppStorage CreateAppStorage,
        ILoadExternalSecretsWorker LoadExternalSecrets
    )
    : ICreate
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig)
        {
            logger.LogInformation("Creating app resources.");

            await runInfoLogger.Log();
            await CreateAppVault.Run(config, azureKeyVaultConfig);
            await CreateApp.Run(config, resources, azureKeyVaultConfig);
            await CreateAppSqlDatabase.Run(config, resources, azureKeyVaultConfig);
            await CreateAppStorage.Run(config, resources, azureKeyVaultConfig, azureStorageConfig);
            await CreateAppCertificate.Run(config, resources, azureKeyVaultConfig);
            await LoadExternalSecrets.Run(config, resources, azureKeyVaultConfig);
        }
    }
}
