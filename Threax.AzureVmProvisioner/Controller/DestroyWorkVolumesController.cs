using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record DestroyWorkVolumesController
    (
        ILogger<DestroyWorkVolumesController> logger, 
        IShellRunner shellRunner
    ) : IController
    {
        public Task Run()
        {
            shellRunner.RunProcessVoid($"docker volume remove threax-provision-azurevm-home", "An error occured removing home volume.");
            shellRunner.RunProcessVoid($"docker volume remove threax-provision-azurevm-temp", "An error occured removing temp volume.");

            return Task.CompletedTask;
        }
    }
}
