using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.ConsoleApp;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IConnectSsh : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Open a ssh window to the server.")]
    record ConnectSsh
    (
        ILogger<SetSecret> logger,
        ISshCredsManager SshCredsManager
    )
    : IConnectSsh
    {
        public async Task Run(Configuration config)
        {
            await SshCredsManager.OpenSshShell();
        }
    }
}
