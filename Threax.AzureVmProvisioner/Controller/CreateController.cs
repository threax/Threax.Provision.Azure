using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    class CreateController : IController
    {
        private readonly IShellRunner shell;
        private readonly EnvironmentConfiguration environmentConfiguration;

        public CreateController
        (
            IShellRunner shell,
            EnvironmentConfiguration environmentConfiguration
        )
        {
            this.shell = shell;
            this.environmentConfiguration = environmentConfiguration;
        }

        public Task Run()
        {
            var rg = environmentConfiguration.ResourceGroup;
            var loc = environmentConfiguration.Location;

            var exists = shell.RunProcess<bool>($"az group exists --name {rg}");
            if (!exists)
            {
                var createdRg = shell.RunProcess($"az group create --name {rg} --location {loc}");
            }

            return Task.CompletedTask;
        }
    }
}
