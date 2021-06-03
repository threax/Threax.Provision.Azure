using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IPush : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Deploy the docker image for the app.")]
    record Push
    (
        ILogger<Deploy> logger,
        IImageManager imageManager,
        IShellRunner shellRunner,
        IVmCommands vmCommands,
        IPathHelper pathHelper,
        IConfigLoader configLoader,
        IKeyVaultManager keyVaultManager,
        ICreateAppSecrets createAppSecrets,
        IRegisterIdServer registerIdServer
    ) : IPush
    {
        public async Task Run(Configuration config) 
        {
            var envConfig = config.Environment;
            var buildConfig = config.Build;

            var image = buildConfig.ImageName;
            var currentTag = buildConfig.GetCurrentTag();
            var taggedImageName = imageManager.FindLatestImage(image, buildConfig.BaseTag, currentTag);
            var finalTag = $"{envConfig.AcrName.ToLowerInvariant()}.azurecr.io/{image}:{buildConfig.Branch}";

            //Push
            logger.LogInformation($"Pushing '{image}' for branch '{buildConfig.Branch}'.");

            shellRunner.RunProcessVoid($"docker tag {taggedImageName} {finalTag}", invalidExitCodeMessage: "An error occured during the docker tag.");

            shellRunner.RunProcessVoid($"docker push {finalTag}", invalidExitCodeMessage: "An error occured during the docker push.");

            await createAppSecrets.Run(config);
        }
    }
}
