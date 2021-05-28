using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IDestroyWorkVolumesController : IController
    {
        Task Run();
    }

    [HelpInfo(HelpCategory.Primary, "Destroy the work volumes used for running the provisioner in a container.")]
    record DestroyWorkVolumesController
    (
        ILogger<DestroyWorkVolumesController> logger, 
        IShellRunner shellRunner
    ) : IDestroyWorkVolumesController
    {
        public Task Run()
        {
            shellRunner.RunProcessVoid($"docker volume remove threax-provision-azurevm-home", "An error occured removing home volume.");
            shellRunner.RunProcessVoid($"docker volume remove threax-provision-azurevm-temp", "An error occured removing temp volume.");

            return Task.CompletedTask;
        }
    }
}
