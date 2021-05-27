using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateWorkVolumesController
    (
        ILogger<CreateWorkVolumesController> logger, 
        IShellRunner shellRunner
    ) : IController
    {
        public Task Run()
        {
            shellRunner.RunProcessVoid($"docker volume create threax-provision-azurevm-home", "An error occured creating home volume.");
            shellRunner.RunProcessVoid($"docker volume create threax-provision-azurevm-temp", "An error occured creating temp volume.");

            return Task.CompletedTask;
        }
    }
}
