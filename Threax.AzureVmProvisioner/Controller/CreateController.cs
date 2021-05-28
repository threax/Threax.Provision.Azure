using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Workers;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateController
    (
        ILogger<CreateController> logger,
        IWorker<RunInfoLogger> runInfoLogger,
        IWorker<CreateAppCertificate> CreateAppCertificate,
        ICreateAppController CreateApp,
        IWorker<CreateAppVault> CreateAppVault,
        IWorker<CreateAppSqlDatabase> CreateAppSqlDatabase,
        IWorker<CreateAppStorage> CreateAppStorage,
        IWorker<LoadExternalSecretsWorker> LoadExternalSecrets
    )
    : IController
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            logger.LogInformation("Creating app resources.");

            await runInfoLogger.ExecuteAsync();
            await CreateAppVault.ExecuteAsync();
            await CreateApp.Run(config, resources, azureKeyVaultConfig);
            await CreateAppSqlDatabase.ExecuteAsync();
            await CreateAppStorage.ExecuteAsync();
            await CreateAppCertificate.ExecuteAsync();
            await LoadExternalSecrets.ExecuteAsync();
        }
    }
}
