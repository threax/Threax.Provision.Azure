using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.AzureVmProvisioner.Workers;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateController : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Create all the resources for an individual app.")]
    record CreateController
    (
        ILogger<CreateController> logger,
        IRunInfoLogger runInfoLogger,
        ICreateAppCertificate CreateAppCertificate,
        ICreateAppController CreateApp,
        ICreateAppVault CreateAppVault,
        ICreateAppSqlDatabase CreateAppSqlDatabase,
        ICreateAppStorage CreateAppStorage,
        ILoadExternalSecretsWorker LoadExternalSecrets
    )
    : ICreateController
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
