﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record DeployController
    (
        EnvironmentConfiguration config,
        ResourceConfiguration resourceConfiguration,
        BuildConfig buildConfig,
        ILogger<DeployController> logger,
        IImageManager imageManager,
        IShellRunner shellRunner,
        IVmCommands vmCommands,
        IPathHelper pathHelper,
        IConfigLoader configLoader
    ) : IController
    {
        public async Task Run()
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
            var branchTag = $"{image}:{buildConfig.Branch}";

            //Push
            logger.LogInformation($"Pushing '{image}' for branch '{buildConfig.Branch}'.");

            shellRunner.RunProcessVoid($"docker tag {taggedImageName} {branchTag}", invalidExitCodeMessage: "An error occured during the docker tag.");

            shellRunner.RunProcessVoid($"docker push {branchTag}", invalidExitCodeMessage: "An error occured during the docker push.");

            //Deploy
            logger.LogInformation($"Deploying '{image}' for branch '{buildConfig.Branch}'.");
            var jobj = configLoader.LoadConfig(); //Get a fresh config copy

            var deploy = jobj["Deploy"];
            if (deploy == null)
            {
                deploy = new JObject();
                jobj["Deploy"] = deploy;
            }
            deploy["ImageName"] = branchTag;

            var fileName = Path.GetFileName(pathHelper.ConfigPath);
            var configJson = jobj.ToString(Newtonsoft.Json.Formatting.Indented);
            await vmCommands.ThreaxDockerToolsRun($"/app/{resource.Name}/{fileName}", configJson);
        }
    }
}
