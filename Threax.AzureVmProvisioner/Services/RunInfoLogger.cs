using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    record RunInfoLogger
    (
        ILogger<RunInfoLogger> Logger,
        IPathHelper PathHelper
    ) : IRunInfoLogger
    {
        public Task Log()
        {
            Logger.LogInformation($"Using config file: '{PathHelper.ConfigPath}'");

            return Task.CompletedTask;
        }
    }
}
