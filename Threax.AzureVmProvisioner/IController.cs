using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner
{
    interface IController
    {
        Task Run();
    }
}
