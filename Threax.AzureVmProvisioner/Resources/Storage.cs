using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Resources
{
    class Storage
    {
        /// <summary>
        /// The name of the secret to create for the storage account access key.
        /// </summary>
        public String AccessKeySecretName { get; set; }

        /// <summary>
        /// The name of the secret to create for the storage account access key.
        /// </summary>
        public String ToolsAccessKeySecretName { get; set; }
    }
}
