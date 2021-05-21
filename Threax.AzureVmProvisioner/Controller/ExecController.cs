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
        ResourceConfiguration resources,
        ISshCredsManager sshCredsManager
    )
    : IController
    {
        private const string FileType = "file";

        public async Task Run()
        {
            var serverSideFilesToRemove = new List<String>();
            try
            {
                var resource = resources.Compute;

                if (String.IsNullOrEmpty(resource.Name))
                {
                    throw new InvalidOperationException("You must include a resource 'Name' property to deploy compute.");
                }

                var command = argsProvider.Args[2];

                logger.LogInformation($"Running exec command '{command}' on remote server.");

                var args = argsProvider.Args.Skip(3).ToArray();

                var realArgs = new List<String>();
                var i = -1;
                while (++i < args.Length)
                {
                    var currentArg = args[i];
                    //Load optional file arguments, any position can start with --exec-load to load a secret and use its path in the final argument list
                    if (currentArg == "--exec-load")
                    {
                        var type = args.Length > ++i ? args[i] : throw new InvalidOperationException($"You must include a type argument in position {i}.");
                        var dest = args.Length > ++i ? args[i] : throw new InvalidOperationException($"You must include a destination argument in position {i}.");
                        var source = args.Length > ++i ? args[i] : throw new InvalidOperationException($"You must include a source argument in position {i}.");
                        var realSource = source;

                        if (type == FileType)
                        {
                            var serverSource = $"~/{Guid.NewGuid()}";
                            await sshCredsManager.CopySshFile(source, serverSource);
                            realSource = serverSource;
                            serverSideFilesToRemove.Add(serverSource);
                        }

                        realArgs.Add(currentArg);
                        realArgs.Add(type);
                        realArgs.Add(dest);
                        realArgs.Add(realSource);
                        //If secret name is passed, it will be handled by the next loop iteration and the else, does not need to be managed here
                    }
                    else
                    {
                        realArgs.Add(currentArg);
                    }
                }

                var fileName = Path.GetFileName(pathHelper.ConfigPath);
                await vmCommands.ThreaxDockerToolsExec($"/app/{resource.Name}/{fileName}", command, realArgs);
            }
            finally
            {
                //Remove server side files
                foreach(var file in serverSideFilesToRemove)
                {
                    try
                    {
                        await sshCredsManager.RunSshCommand($"rm -f \"{file}\"");
                    }
                    catch(Exception ex)
                    {
                        logger.LogError($"{ex.GetType().Name} erasing server side file '{file}'. Message: '{ex.Message}'. Please remove file manually.");
                    }
                }
            }
        }
    }
}
