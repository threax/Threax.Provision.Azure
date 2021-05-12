using System;
using System.Collections.Generic;
using System.Text;
using Threax.Provision.AzPowershell;
using Threax.Provision.Azure.Core;

namespace Threax.AzureVmProvisioner.ArmTemplates.SqlDb
{
    class ArmSqlDb : ArmTemplate
    {
        public ArmSqlDb(String serverName, String databaseName)
        {
            this.serverName = serverName;
            this.databaseName = databaseName;
        }

        public string databaseName { get; set; }

        public string serverName { get; set; }
    }
}
