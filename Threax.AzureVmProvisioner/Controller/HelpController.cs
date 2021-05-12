using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Controller
{
    class HelpController : IController
    {
        public Task Run()
        {
            return Task.CompletedTask;
        }
    }
}
