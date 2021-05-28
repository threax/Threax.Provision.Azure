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
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Create all the resources for an individual app.")]
    record CreateController
    (
        ILogger<CreateController> logger,
        IRunInfoLogger runInfoLogger,
        IWorker<CreateAppCertificate> CreateAppCertificate,
        ICreateAppController CreateApp,
        IWorker<CreateAppVault> CreateAppVault,
        IWorker<CreateAppSqlDatabase> CreateAppSqlDatabase,
        IWorker<CreateAppStorage> CreateAppStorage,
        IWorker<LoadExternalSecretsWorker> LoadExternalSecrets
    )
    : ICreateController
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            logger.LogInformation("Creating app resources.");

            await runInfoLogger.Log();
            await CreateAppVault.ExecuteAsync();
            await CreateApp.Run(config, resources, azureKeyVaultConfig);
            await CreateAppSqlDatabase.ExecuteAsync();
            await CreateAppStorage.ExecuteAsync();
            await CreateAppCertificate.ExecuteAsync();
            await LoadExternalSecrets.ExecuteAsync();
        }
    }
}
