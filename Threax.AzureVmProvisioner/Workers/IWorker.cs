using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Workers
{
    public interface IWorker<T>
    {
        public Task ExecuteAsync();
    }
}
