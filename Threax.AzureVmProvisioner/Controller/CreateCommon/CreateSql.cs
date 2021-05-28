using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.SqlDb;
using Threax.AzureVmProvisioner.ArmTemplates.SqlServer;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    interface ICreateSql : IController
    {
        Task Run(EnvironmentConfiguration config);
    }

    [HelpInfo(HelpCategory.CreateCommon, "Create the common sql server and shared db instance for all apps to use.")]
    record CreateSql
    (
        ILogger<CreateSql> logger,
        IArmTemplateManager armTemplateManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ICredentialLookup credentialLookup
    ) : ICreateSql
    {
        public async Task Run(EnvironmentConfiguration config)
        {
            //In this setup there is actually only 1 db to save money.
            //So both the sql server and the db will be provisioned in this step.
            //You would want to have separate dbs in a larger setup.
            await keyVaultAccessManager.Unlock(config.InfraKeyVaultName, config.UserId);

            var saCreds = await credentialLookup.GetOrCreateCredentials(config.InfraKeyVaultName, config.SqlSaBaseKey);

            //Setup logical server
            logger.LogInformation($"Setting up SQL Logical Server '{config.SqlServerName}' in Resource Group '{config.ResourceGroup}'.");
            await this.armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, new ArmSqlServer(config.SqlServerName, saCreds.User, saCreds.Pass, config.VnetName, config.VnetSubnetName));

            //Setup shared sql db
            logger.LogInformation($"Setting up Shared SQL Database '{config.SqlDbName}' on SQL Logical Server '{config.SqlServerName}'.");
            await this.armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, new ArmSqlDb(config.SqlServerName, config.SqlDbName));
        }
    }
}
