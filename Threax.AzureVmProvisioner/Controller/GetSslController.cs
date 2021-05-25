using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Workers;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record GetSslController
    (
        BuildConfig appConfig, 
        ILogger<CloneController> logger, 
        IShellRunner shellRunner,
        IWorker<GetSslCertificate> CreateSslCertificate
    ) 
    : IController
    {
        public async Task Run()
        {
            await CreateSslCertificate.ExecuteAsync();
        }
    }
}
