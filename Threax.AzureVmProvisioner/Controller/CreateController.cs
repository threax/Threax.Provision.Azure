using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateController
    (
        IShellRunner shell,
        EnvironmentConfiguration environmentConfiguration
    )
    : IController
    {
        public async Task Run()
        {
            
        }
    }
}
