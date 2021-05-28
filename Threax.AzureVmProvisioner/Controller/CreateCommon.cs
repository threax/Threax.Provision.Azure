using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateCommon : IController
    {
        Task Run(EnvironmentConfiguration config);
    }

    [HelpInfo(HelpCategory.Primary, "Create common resources needed by all apps.")]
    record CreateCommon
    (
        ILogger<CreateCommon> logger,
        IRunInfoLogger runInfoLogger,
        ICreateResourceGroup createResourceGroup,
        ICreateInfraKeyVault createInfraKeyVault,
        ICreateVM createVm,
        ICreateSql createSql
    )
    : ICreateCommon
    {
        public async Task Run(EnvironmentConfiguration config)
        {
            logger.LogInformation("Creating common resources.");

            await runInfoLogger.Log();
            await createResourceGroup.Run(config);
            await createInfraKeyVault.Run(config);
            await createVm.Run(config); //This must be synchronous with the db since it creates the vnet. That needs to be separated out
            await createSql.Run(config);
        }
    }
}
