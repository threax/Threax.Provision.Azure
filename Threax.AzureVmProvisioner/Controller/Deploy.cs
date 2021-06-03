using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IDeploy : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Deploy the docker image for the app.")]
    record Deploy
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
    ) : IDeploy
    {
        public async Task Run(Configuration config) 
        {
            var envConfig = config.Environment;
            var resourceConfiguration = config.Resources;
            var buildConfig = config.Build;
        
            var resource = resourceConfiguration.Compute;

            if (resource == null)
            {
                throw new InvalidOperationException($"You must supply a '{nameof(resourceConfiguration.Compute)}' property in your resource configuration.");
            }

            if (String.IsNullOrEmpty(resource.Name))
            {
                throw new InvalidOperationException("You must include a resource 'Name' property to deploy compute.");
            }

            var image = buildConfig.ImageName;
            var finalTag = $"{envConfig.AcrName.ToLowerInvariant()}.azurecr.io/{image}:{buildConfig.Branch}";

            //Deploy
            logger.LogInformation($"Deploying '{image}' for branch '{buildConfig.Branch}'.");
            var jobj = configLoader.LoadConfig(config.GetConfigPath()); //Get a fresh config copy

            var deploy = jobj["Deploy"];
            if (deploy == null)
            {
                deploy = new JObject();
                jobj["Deploy"] = deploy;
            }
            deploy["ImageName"] = finalTag;

            var fileName = Path.GetFileName(config.GetConfigPath());
            var configJson = jobj.ToString(Newtonsoft.Json.Formatting.Indented);
            await vmCommands.ThreaxDockerToolsRun($"/app/{resource.Name}/{fileName}", configJson);

            await registerIdServer.Run(config);
        }
    }
}
