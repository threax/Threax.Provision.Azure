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
        Task Run(Configuration config);
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
        public async Task Run(Configuration config)
        {
            var create = CreateController.Run(config);

            await CloneController.Run(config);
            await BuildController.Run(config);

            await create;

            await DeployController.Run(config);
        }
    }
}
