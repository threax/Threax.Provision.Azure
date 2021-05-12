using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.AzureVmProvisioner
{
    /// <summary>
    /// Centralized configuration that applies to all resources in an particular environment.
    /// </summary>
    class EnvironmentConfiguration
    {
        public String Location { get; set; }

        public String ResourceGroup { get; set; }
    }
}
