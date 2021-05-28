using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IManageApp : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig, BuildConfig buildConfig, DeploymentConfig deploymentConfig);
    }

    [HelpInfo(HelpCategory.Primary, "Do all steps needed to get an app up and running.")]
    record ManageApp
    (
        ICreate CreateController,
        IClone CloneController,
        IBuild BuildController,
        IDeploy DeployController
    ) : IManageApp
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig, BuildConfig buildConfig, DeploymentConfig deploymentConfig)
        {
            var create = CreateController.Run(config, resources, azureKeyVaultConfig, azureStorageConfig);

            await CloneController.Run(buildConfig);
            await BuildController.Run(buildConfig);

            await create;

            await DeployController.Run(config, resources, buildConfig, azureKeyVaultConfig, deploymentConfig);
        }
    }
}
