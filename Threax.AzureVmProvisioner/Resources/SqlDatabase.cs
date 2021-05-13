using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Resources
{
    public class SqlDatabase
    {
        public String Name { get; set; }

        public String ConnectionStringName { get; set; }

        public String OwnerConnectionStringName { get; set; }
    }
}
