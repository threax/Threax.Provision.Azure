using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IBuildAndDeploy : IController
    {
        Task Run(Configuration configuration);
    }

    record BuildAndDeploy
    (
        IBuild Build,
        IPush Push,
        IDeploy Deploy
    ) : IBuildAndDeploy
    {
        public async Task Run(Configuration configuration)
        {
            await Build.Run(configuration);
            await Push.Run(configuration);
            await Deploy.Run(configuration);
        }
    }
}
