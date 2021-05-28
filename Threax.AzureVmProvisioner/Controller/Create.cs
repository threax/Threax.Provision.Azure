using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreate : IController
    {
        Task Run(Configuration config);
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
        public async Task Run(Configuration config)
        {
            logger.LogInformation("Creating app resources.");

            await runInfoLogger.Log(config);
            await CreateAppVault.Run(config);
            await Task.WhenAll
            (
                CreateApp.Run(config),
                CreateAppSqlDatabase.Run(config),
                CreateAppStorage.Run(config),
                CreateAppCertificate.Run(config),
                LoadExternalSecrets.Run(config)
            );
        }
    }
}
