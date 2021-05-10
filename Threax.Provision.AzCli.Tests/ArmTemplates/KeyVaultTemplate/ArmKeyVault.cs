using System;
using System.Collections.Generic;
using System.Text;
using Threax.Provision.Azure.Core;

namespace Threax.Provision.AzCli.Tests.ArmTemplates.KeyVaultTemplate
{
    class ArmKeyVault : ArmTemplate
    {
        public ArmKeyVault(String name, String location, String tenant)
        {
            this.name = name;
            this.location = location;
            this.tenant = tenant;
        }

        public String name { get; set; }

        public string location { get; }

        public string tenant { get; }
    }
}
