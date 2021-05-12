﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.AzureVmProvisioner.Resources
{
    /// <summary>
    /// Configuration for a single resource in the system.
    /// </summary>
    class ResourceConfiguration
    {
        /// <summary>
        /// The configuration for the compute the resource runs on.
        /// </summary>
        public Compute Compute { get; set; }
    }
}
