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

        /// <summary>
        /// Settings for an app's sql database.
        /// </summary>
        public SqlDatabase SqlDatabase { get; set; }

        /// <summary>
        /// Settings for an app's storage.
        /// </summary>
        public Storage Storage { get; set; }

        /// <summary>
        /// Configuration for an app's certificate.
        /// </summary>
        public Certificate Certificate { get; set; }

        /// <summary>
        /// Any secrets from the external key vault that should link to this app.
        /// </summary>
        public List<ExternalSecret> ExternalSecrets { get; set; }

        /// <summary>
        /// The registration for the app in the id server.
        /// </summary>
        public IdServerRegistration IdServerRegistration { get; set; }
    }
}
