using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    record LoginController
    (
        EnvironmentConfiguration config, 
        IShellRunner shellRunner,
        ILogger<LoginController> logger
    )
    : IController
    {
        public async Task Run()
        {
            shellRunner.RunProcessVoid($"Connect-AzAccount -UseDeviceAuthentication", invalidExitCodeMessage: $"Error logging into ACR '{config.AcrName}'.");
        }
    }
}
