using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.AzureVmProvisioner.Resources
{
    /// <summary>
    /// Configuration for compute instances.
    /// </summary>
    class Compute
    {
        /// <summary>
        /// The name of the compute.
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// The name of the app insights secret to store the instrumentation key under.
        /// </summary>
        public String AppInsightsSecretName { get; set; }
    }
}
