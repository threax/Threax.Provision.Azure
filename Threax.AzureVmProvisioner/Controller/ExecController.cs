using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.ConsoleApp;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record ExecController
    (
        ILogger<ExecController> logger,
        IVmCommands vmCommands,
        IPathHelper pathHelper,
        IArgsProvider argsProvider,
        ResourceConfiguration resources
    )
    : IController
    {
        public async Task Run()
        {
            var resource = resources.Compute;

            if (String.IsNullOrEmpty(resource.Name))
            {
                throw new InvalidOperationException("You must include a resource 'Name' property to deploy compute.");
            }

            var command = argsProvider.Args[2];

            logger.LogInformation($"Running exec command '{command}'.");

            var args = argsProvider.Args.Skip(3).ToArray();
            var fileName = Path.GetFileName(pathHelper.ConfigPath);
            await vmCommands.ThreaxDockerToolsExec($"/app/{resource.Name}/{fileName}", command, args);
        }
    }
}
