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
    interface IClone : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Clone the repository for the app.")]
    record Clone
    (
        ILogger<Clone> logger, 
        IShellRunner shellRunner
    ) 
    : IClone
    {
        public async Task Run(Configuration config)
        {
            var buildConfig = config.Build;

            if (String.IsNullOrEmpty(buildConfig.RepoUrl))
            {
                logger.LogInformation($"No '{nameof(buildConfig.RepoUrl)}' Not cloning anything for current app.");
                return;
            }

            var clonePath = Path.GetFullPath(buildConfig.ClonePath);
            var repo = buildConfig.RepoUrl;

            if (Directory.Exists(buildConfig.ClonePath))
            {
                logger.LogInformation($"Pulling changes to {clonePath}");
                await shellRunner.RunProcessVoidAsync($"cd {buildConfig.ClonePath};git pull", invalidExitCodeMessage: $"Error pulling repository '{clonePath}'.");
            }
            else
            {
                logger.LogInformation($"Cloning {repo} to {clonePath}");
                await shellRunner.RunProcessVoidAsync($"git clone {repo} {clonePath}", invalidExitCodeMessage: $"Error cloning repository '{repo}' to '{clonePath}'.");
            }
        }
    }
}
