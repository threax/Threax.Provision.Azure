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
    interface IBuildController : IController
    {
        Task Run(BuildConfig buildConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Build the docker image for the app.")]
    record BuildController
    (
        ILogger<BuildController> logger, 
        IShellRunner shellRunner
    ) : IBuildController
    {
        public async Task Run(BuildConfig buildConfig)
        {
            var context = buildConfig.GetContext();
            var dockerFile = Path.GetFullPath(Path.Combine(context, buildConfig.Dockerfile ?? throw new InvalidOperationException($"Please provide {nameof(buildConfig.Dockerfile)} when using build.")));
            var image = buildConfig.ImageName;
            var buildTag = buildConfig.GetBuildTag();
            var currentTag = buildConfig.GetCurrentTag();

            var tag1 = $"{image}:{buildTag}";
            var tag2 = $"{image}:{currentTag}";

            List<FormattableString> command = new List<FormattableString>(){ $"docker build {context} -f {dockerFile} -t {tag1} -t {tag2} --progress=plain" };

            if (buildConfig.PullAllImages)
            {
                command.Add($" --pull");
            }

            if (buildConfig.Args != null)
            {
                foreach (var arg in buildConfig.Args)
                {
                    var argStr = $"{arg.Key}={arg.Value}";
                    command.Add($" --build-arg {argStr}");
                }
            }

            var exitCode = await shellRunner.RunProcessGetExitAsync(command);
            if (exitCode != 0)
            {
                throw new InvalidOperationException("An error occured during the docker build.");
            }
        }
    }
}
