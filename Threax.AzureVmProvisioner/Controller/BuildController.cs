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
    record BuildController
    (
        BuildConfig buildConfig, 
        ILogger<BuildController> logger, 
        IShellRunner shellRunner
    ) : IController
    {
        public Task Run()
        {
            var context = buildConfig.GetContext();
            var dockerFile = Path.GetFullPath(Path.Combine(context, buildConfig.Dockerfile ?? throw new InvalidOperationException($"Please provide {nameof(buildConfig.Dockerfile)} when using build.")));
            var image = buildConfig.ImageName;
            var buildTag = buildConfig.GetBuildTag();
            var currentTag = buildConfig.GetCurrentTag();

            List<FormattableString> command = new List<FormattableString>(){ $"$env:DOCKER_BUILDKIT=1; docker build {context} -f {dockerFile} -t {image}:{buildTag} -t {image}:{currentTag} --progress=plain" };

            if (buildConfig.PullAllImages)
            {
                command.Add($" --pull");
            }

            var exitCode = shellRunner.RunProcessGetExit(command);
            if (exitCode != 0)
            {
                throw new InvalidOperationException("An error occured during the docker build.");
            }

            return Task.CompletedTask;
        }
    }
}
