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
        /// <summary>
        /// The location of the resources.
        /// </summary>
        public String Location { get; set; }

        /// <summary>
        /// The resource group to create resources in.
        /// </summary>
        public String ResourceGroup { get; set; }

        /// <summary>
        /// The current subscription id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// The Active Directory TenantId
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// The name of the key vault to create for general infrastructure that doesn't belong to an app.
        /// </summary>
        public String InfraKeyVaultName { get; set; }

        /// <summary>
        /// The name of the key vault to create for secrets that exist outside the automation. This can be left in place
        /// when environments are destroyed so you don't have to keep getting things like ssl certs or other secrets that
        /// might be limited our outside our our control.
        /// </summary>
        public String ExternalKeyVaultName { get; set; }

        /// <summary>
        /// The guid of the Azure Devops user to set permissions for.
        /// </summary>
        public Guid? AzDoUser { get; set; }

        /// <summary>
        /// The name of the acr to create.
        /// </summary>
        public String AcrName { get; set; }

        /// <summary>
        /// The name of the vm to provision.
        /// </summary>
        public String VmName { get; set; }

        /// <summary>
        /// The current user id. This should be changed to auto discovery, but that is hard.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The name of the public ip for the vm.
        /// </summary>
        public String PublicIpName { get; set; }

        /// <summary>
        /// The name of the Network Security Group.
        /// </summary>
        public String NsgName { get; set; }

        /// <summary>
        /// The name of the vnet.
        /// </summary>
        public String VnetName { get; set; }

        /// <summary>
        /// The name of the vnet subnet. Default: 'default'
        /// </summary>
        public String VnetSubnetName { get; set; } = "default";

        /// <summary>
        /// The base name of the sa secret in the key vault. Default: "sqlsrv-sa"
        /// </summary>
        public string SqlSaBaseKey { get; set; } = "sqlsrv-sa";

        /// <summary>
        /// The name of the sql server to create.
        /// </summary>
        public String SqlServerName { get; set; }

        /// <summary>
        /// The name of the sql database to create. This setup shares 1 db for all apps to save money.
        /// </summary>
        public String SqlDbName { get; set; }

        /// <summary>
        /// The base name of the secret in the infra kv for the vm admin. Default: 'vm-admin'
        /// </summary>
        public string VmAdminBaseKey { get; set; } = "vm-admin";

        /// <summary>
        /// The name of the key in the infra key vault that holds the known hosts key. Default: 'known-hosts'
        /// </summary>
        public string SshKnownHostKey { get; set; } = "known-hosts";

        /// <summary>
        /// The name of the shared app insights.
        /// </summary>
        public String AppInsightsName { get; set; }

        /// <summary>
        /// The url to add app subdomains to.
        /// </summary>
        public String BaseUrl { get; set; }

        /// <summary>
        /// The email address to use when applying for ssl certificates.
        /// </summary>
        public String SslEmail { get; set; }

        /// <summary>
        /// The name of the secret that stores the ssl cert in the infra key vault. Default: ssl-public-key
        /// </summary>
        public String SslPublicKeyName { get; set; } = "ssl-public-key";

        /// <summary>
        /// The name of the secret that stores teh ssl cert private key in the key vault. Default: ssl-private-key
        /// </summary>
        public String SslPrivateKeyName { get; set; } = "ssl-private-key";
    }
}
