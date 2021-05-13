using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Workers;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateController
    (
        ILogger<CreateCommonController> logger,
        IWorker<RunInfoLogger> runInfoLogger,
        IWorker<CreateAppCertificate> CreateAppCertificate,
        IWorker<CreateApp> CreateApp,
        IWorker<CreateAppVault> CreateAppVault,
        IWorker<CreateAppSqlDatabase> CreateAppSqlDatabase,
        IWorker<CreateAppStorage> CreateAppStorage
    )
    : IController
    {
        public async Task Run()
        {
            logger.LogInformation("Creating app resources.");

            await runInfoLogger.ExecuteAsync();
            await CreateAppVault.ExecuteAsync();
            await CreateApp.ExecuteAsync();
            await CreateAppSqlDatabase.ExecuteAsync();
            await CreateAppStorage.ExecuteAsync();
            await CreateAppCertificate.ExecuteAsync();
        }
    }
}
