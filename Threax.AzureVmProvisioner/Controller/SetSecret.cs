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
    interface ISetSecret : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Set a secret in the linked app's key vault.")]
    record SetSecret
    (
        ILogger<SetSecret> logger,
        IVmCommands vmCommands,
        IArgsProvider argsProvider,
        IPathHelper pathHelper
    )
    : ISetSecret
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var resources = config.Resources;

            var resource = resources.Compute;

            var args = argsProvider.Args.Skip(2).ToList();
            if (args.Count < 2)
            {
                throw new InvalidOperationException("You must provide a name and source file to set a secret.");
            }

            var name = args[0];
            var source = args[1];

            var fileName = Path.GetFileName(config.GetConfigPath());
            var configFilePath = $"/app/{resource.Name}/{fileName}";

            await vmCommands.SetSecretFromFile(envConfig.VmName, envConfig.ResourceGroup, config.GetConfigPath(), configFilePath, name, source);
        }
    }
}
