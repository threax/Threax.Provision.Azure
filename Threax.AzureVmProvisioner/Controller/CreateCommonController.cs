using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Workers;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateCommonController
    (
        ILogger<CreateCommonController> logger,
        EnvironmentConfiguration config,
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

            await createResourceGroup.ExecuteAsync();
            await createInfraKeyVault.ExecuteAsync();
            await createVm.ExecuteAsync();
            await createSql.ExecuteAsync();
        }
    }
}
