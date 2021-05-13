using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Workers;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateCommonController
    (
        ILogger<CreateCommonController> logger,
        IWorker<RunInfoLogger> runInfoLogger,
        IWorker<CreateResourceGroup> createResourceGroup,
        IWorker<CreateInfraKeyVault> createInfraKeyVault,
        IWorker<CreateVM> createVm,
        IWorker<CreateSql> createSql
    )
    : IController
    {
        public async Task Run()
        {
            logger.LogInformation("Creating common resources.");

            await runInfoLogger.ExecuteAsync();
            await createResourceGroup.ExecuteAsync();
            await createInfraKeyVault.ExecuteAsync();
            await createVm.ExecuteAsync();
            await createSql.ExecuteAsync();
        }
    }
}
