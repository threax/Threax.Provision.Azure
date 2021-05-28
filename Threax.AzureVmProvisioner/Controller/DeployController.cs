using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    record DeployController
    (
        ILogger<DeployController> logger,
        IImageManager imageManager,
        IShellRunner shellRunner,
        IVmCommands vmCommands,
        IPathHelper pathHelper,
        IConfigLoader configLoader,
        IKeyVaultManager keyVaultManager,
        ICreateAppSecrets createAppSecrets,
        IRegisterIdServer registerIdServer
    ) : IController
    {
        public async Task Run(
        EnvironmentConfiguration config,
        ResourceConfiguration resourceConfiguration,
        BuildConfig buildConfig,
        AzureKeyVaultConfig azureKeyVaultConfig,
        DeploymentConfig deploymentConfig)
        {
            var resource = resourceConfiguration.Compute;

            if(resource == null)
            {
                throw new InvalidOperationException($"You must supply a '{nameof(resourceConfiguration.Compute)}' property in your resource configuration.");
            }

            if (String.IsNullOrEmpty(resource.Name))
            {
                throw new InvalidOperationException("You must include a resource 'Name' property to deploy compute.");
            }

            var image = buildConfig.ImageName;
            var currentTag = buildConfig.GetCurrentTag();
            var taggedImageName = imageManager.FindLatestImage(image, buildConfig.BaseTag, currentTag);
            var finalTag = $"{config.AcrName.ToLowerInvariant()}.azurecr.io/{image}:{buildConfig.Branch}";

            //Push
            logger.LogInformation($"Pushing '{image}' for branch '{buildConfig.Branch}'.");

            shellRunner.RunProcessVoid($"docker tag {taggedImageName} {finalTag}", invalidExitCodeMessage: "An error occured during the docker tag.");

            shellRunner.RunProcessVoid($"docker push {finalTag}", invalidExitCodeMessage: "An error occured during the docker push.");

            await createAppSecrets.Run(config, resourceConfiguration, azureKeyVaultConfig, deploymentConfig);

            //Deploy
            logger.LogInformation($"Deploying '{image}' for branch '{buildConfig.Branch}'.");
            var jobj = configLoader.LoadConfig(); //Get a fresh config copy

            var deploy = jobj["Deploy"];
            if (deploy == null)
            {
                deploy = new JObject();
                jobj["Deploy"] = deploy;
            }
            deploy["ImageName"] = finalTag;

            var fileName = Path.GetFileName(pathHelper.ConfigPath);
            var configJson = jobj.ToString(Newtonsoft.Json.Formatting.Indented);
            await vmCommands.ThreaxDockerToolsRun($"/app/{resource.Name}/{fileName}", configJson);

            await registerIdServer.Run(config, resourceConfiguration, azureKeyVaultConfig, deploymentConfig);
        }
    }
}
