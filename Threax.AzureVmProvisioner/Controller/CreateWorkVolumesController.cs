using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateWorkVolumesController : IController
    {
        Task Run();
    }

    [HelpInfo(HelpCategory.Primary, "Create the work volumes used for running the provisioner in a container.")]
    record CreateWorkVolumesController
    (
        ILogger<CreateWorkVolumesController> logger, 
        IShellRunner shellRunner
    ) : ICreateWorkVolumesController
    {
        public Task Run()
        {
            shellRunner.RunProcessVoid($"docker volume create threax-provision-azurevm-home", "An error occured creating home volume.");
            shellRunner.RunProcessVoid($"docker volume create threax-provision-azurevm-temp", "An error occured creating temp volume.");

            return Task.CompletedTask;
        }
    }
}
