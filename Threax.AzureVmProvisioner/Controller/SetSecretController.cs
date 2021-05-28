using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.ConsoleApp;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ISetSecretController : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources);
    }

    [HelpInfo(HelpCategory.Primary, "Set a secret in the linked app's key vault.")]
    record SetSecretController
    (
        ILogger<SetSecretController> logger,
        IVmCommands vmCommands,
        IArgsProvider argsProvider,
        IPathHelper pathHelper
    )
    : ISetSecretController
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources)
        {
            var resource = resources.Compute;

            var args = argsProvider.Args.Skip(2).ToList();
            if (args.Count < 2)
            {
                throw new InvalidOperationException("You must provide a name and source file to set a secret.");
            }

            var name = args[0];
            var source = args[1];

            var fileName = Path.GetFileName(pathHelper.ConfigPath);
            var configFilePath = $"/app/{resource.Name}/{fileName}";

            await vmCommands.SetSecretFromFile(config.VmName, config.ResourceGroup, pathHelper.ConfigPath, configFilePath, name, source);
        }
    }
}
