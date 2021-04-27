using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.Provision.AzCli.Models
{
    public class AccountShowOutput
    {
        public string environmentName { get; set; }
        public string homeTenantId { get; set; }
        public string id { get; set; }
        public bool isDefault { get; set; }
        public string name { get; set; }
        public string state { get; set; }
        public string tenantId { get; set; }
        public User user { get; set; }

        public class User
        {
            public string name { get; set; }
            public string type { get; set; }
        }
    }

}
