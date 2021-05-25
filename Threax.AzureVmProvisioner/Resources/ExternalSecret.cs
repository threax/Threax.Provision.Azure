using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Resources
{
    enum ExternalSecretDestinationType
    {
        /// <summary>
        /// Store the secret in the app's key vault. Default.
        /// </summary>
        AppKeyVault = 0,
        /// <summary>
        /// Store the secret in the app's local secret directory.
        /// </summary>
        Local = 1,
    }

    class ExternalSecret
    {
        /// <summary>
        /// The name of the source secret in the external key vault.
        /// </summary>
        public String Source { get; set; }

        /// <summary>
        /// The name of the destination secret.
        /// </summary>
        public String Destination { get; set; }

        /// <summary>
        /// The destination type of the secret.
        /// </summary>
        public ExternalSecretDestinationType Type { get; set; }
    }
}
