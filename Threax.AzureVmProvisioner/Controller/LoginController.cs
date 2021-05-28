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
    interface ILoginController : IController
    {
        Task Run();
    }

    [HelpInfo(HelpCategory.Primary, "Log into the Azure Services")]
    record LoginController
    (
        EnvironmentConfiguration config, 
        IShellRunner shellRunner,
        ILogger<LoginController> logger
    )
    : ILoginController
    {
        public async Task Run()
        {
            await shellRunner.RunProcessVoidAsync($"Connect-AzAccount -UseDeviceAuthentication", invalidExitCodeMessage: $"Error logging into ACR '{config.AcrName}'.");
        }
    }
}
