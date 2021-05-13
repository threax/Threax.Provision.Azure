using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record CloneController
    (
        BuildConfig appConfig, 
        ILogger<CloneController> logger, 
        IShellRunner shellRunner
    ) 
    : IController
    {
        public Task Run()
        {
            var clonePath = Path.GetFullPath(appConfig.ClonePath);
            var repo = appConfig.RepoUrl;

            if (Directory.Exists(appConfig.ClonePath))
            {
                logger.LogInformation($"Pulling changes to {clonePath}");
                shellRunner.RunProcessVoid($"cd {appConfig.ClonePath};git pull", invalidExitCodeMessage: $"Error pulling repository '{clonePath}'.");
            }
            else
            {
                logger.LogInformation($"Cloning {repo} to {clonePath}");
                shellRunner.RunProcessVoid($"git clone {repo} {clonePath}", invalidExitCodeMessage: $"Error cloning repository '{repo}' to '{clonePath}'.");
            }

            return Task.CompletedTask;
        }
    }
}
