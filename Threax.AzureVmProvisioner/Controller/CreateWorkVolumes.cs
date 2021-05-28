using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateWorkVolumes : IController
    {
        Task Run();
    }

    [HelpInfo(HelpCategory.Primary, "Create the work volumes used for running the provisioner in a container.")]
    record CreateWorkVolumes
    (
        ILogger<CreateWorkVolumes> logger, 
        IShellRunner shellRunner
    ) : ICreateWorkVolumes
    {
        public async Task Run()
        {
            await shellRunner.RunProcessVoidAsync($"docker volume create threax-provision-azurevm-home", "An error occured creating home volume.");
            await shellRunner.RunProcessVoidAsync($"docker volume create threax-provision-azurevm-temp", "An error occured creating temp volume.");
        }
    }
}
