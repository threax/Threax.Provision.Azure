using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;

namespace Threax.AzureVmProvisioner.Workers
{
    record RunInfoLogger
    (
        ILogger<RunInfoLogger> Logger,
        IPathHelper PathHelper
    )
    : IWorker<RunInfoLogger>
    {
        public Task ExecuteAsync()
        {
            Logger.LogInformation($"Using config file: '{PathHelper.ConfigPath}'");

            return Task.CompletedTask;
        }
    }
}
