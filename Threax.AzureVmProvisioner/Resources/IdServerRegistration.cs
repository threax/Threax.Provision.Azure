using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Resources
{
    enum IdServerRegistrationType
    {
        None,
        RegularApp,
        AppDashboard
    }

    class IdServerRegistration
    {
        public String IdServerPath { get; set; } = "threax-id/appsettings.json";

        public IdServerRegistrationType Type { get; set; }

        public string JwtAuthSecretName { get; set; } = "run-secret--JwtAuth--ClientSecret";

        public string ClientCredentialsSecretName { get; set; } = "run-secret--SharedClientCredentials--ClientSecret";
    }
}
