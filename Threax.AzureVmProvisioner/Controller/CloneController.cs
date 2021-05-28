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
    interface ICloneController : IController
    {
        Task Run(BuildConfig appConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Clone the repository for the app.")]
    record CloneController
    (
        ILogger<CloneController> logger, 
        IShellRunner shellRunner
    ) 
    : ICloneController
    {
        public async Task Run(BuildConfig appConfig)
        {
            if (String.IsNullOrEmpty(appConfig.RepoUrl))
            {
                logger.LogInformation($"No '{nameof(appConfig.RepoUrl)}' Not cloning anything for current app.");
                return;
            }

            var clonePath = Path.GetFullPath(appConfig.ClonePath);
            var repo = appConfig.RepoUrl;

            if (Directory.Exists(appConfig.ClonePath))
            {
                logger.LogInformation($"Pulling changes to {clonePath}");
                await shellRunner.RunProcessVoidAsync($"cd {appConfig.ClonePath};git pull", invalidExitCodeMessage: $"Error pulling repository '{clonePath}'.");
            }
            else
            {
                logger.LogInformation($"Cloning {repo} to {clonePath}");
                await shellRunner.RunProcessVoidAsync($"git clone {repo} {clonePath}", invalidExitCodeMessage: $"Error cloning repository '{repo}' to '{clonePath}'.");
            }
        }
    }
}
