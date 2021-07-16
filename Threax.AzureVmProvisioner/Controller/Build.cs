using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IBuild : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Build the docker image for the app.")]
    record Build
    (
        ILogger<Build> logger, 
        IProcessRunner processRunner
    ) : IBuild
    {
        public Task Run(Configuration config)
        {
            var buildConfig = config.Build;

            var context = buildConfig.GetContext();
            var dockerFile = Path.GetFullPath(Path.Combine(context, buildConfig.Dockerfile ?? throw new InvalidOperationException($"Please provide {nameof(buildConfig.Dockerfile)} when using build.")));
            var image = buildConfig.ImageName;
            var buildTag = buildConfig.GetBuildTag();
            var currentTag = buildConfig.GetCurrentTag();

            var processStartInfo = new ProcessStartInfo("docker")
            {
                ArgumentList =
                {
                    "build", context,
                    "-f", dockerFile,
                    "-t", $"{image}:{buildTag}",
                    "-t", $"{image}:{currentTag}",
                    "--progress=plain"
                }
            };

            processStartInfo.EnvironmentVariables.Add("DOCKER_BUILDKIT", "1");

            if (buildConfig.PullAllImages)
            {
                processStartInfo.ArgumentList.Add("--pull");
            }

            if (buildConfig.Args != null)
            {
                foreach (var arg in buildConfig.Args)
                {
                    processStartInfo.ArgumentList.Add("--build-arg");
                    processStartInfo.ArgumentList.Add($"{arg.Key}={arg.Value}");
                }
            }

            processRunner.Run(processStartInfo);

            return Task.CompletedTask;
        }
    }
}
