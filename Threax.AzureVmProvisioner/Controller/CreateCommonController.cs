using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;
using Threax.AzureVmProvisioner.Workers;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateCommonController : IController
    {
        Task Run(EnvironmentConfiguration config);
    }

    [HelpInfo(HelpCategory.Primary, "Create common resources needed by all apps.")]
    record CreateCommonController
    (
        ILogger<CreateCommonController> logger,
        IRunInfoLogger runInfoLogger,
        ICreateResourceGroup createResourceGroup,
        ICreateInfraKeyVault createInfraKeyVault,
        ICreateVM createVm,
        ICreateSql createSql
    )
    : ICreateCommonController
    {
        public async Task Run(EnvironmentConfiguration config)
        {
            logger.LogInformation("Creating common resources.");

            await runInfoLogger.Log();
            await createResourceGroup.Run(config);
            await createInfraKeyVault.Run(config);
            await createVm.Run(config);
            await createSql.Run(config);
        }
    }
}
