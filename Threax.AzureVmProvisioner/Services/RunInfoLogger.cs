using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    interface IRunInfoLogger
    {
        Task Log(Configuration config);
    }

    record RunInfoLogger
    (
        ILogger<RunInfoLogger> Logger,
        IPathHelper PathHelper
    ) : IRunInfoLogger
    {
        public Task Log(Configuration config)
        {
            Logger.LogInformation($"Using config file: '{config.GetConfigPath()}'");

            return Task.CompletedTask;
        }
    }
}
