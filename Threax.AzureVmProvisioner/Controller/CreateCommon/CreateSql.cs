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

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateSql : IController
    {
        Task Run(Configuration config);
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
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;

            //In this setup there is actually only 1 db to save money.
            //So both the sql server and the db will be provisioned in this step.
            //You would want to have separate dbs in a larger setup.
            await keyVaultAccessManager.Unlock(envConfig.InfraKeyVaultName, envConfig.UserId);

            var saCreds = await credentialLookup.GetOrCreateCredentials(envConfig.InfraKeyVaultName, envConfig.SqlSaBaseKey);

            //Setup logical server
            logger.LogInformation($"Setting up SQL Logical Server '{envConfig.SqlServerName}' in Resource Group '{envConfig.ResourceGroup}'.");
            await this.armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, new ArmSqlServer(envConfig.SqlServerName, saCreds.User, saCreds.Pass, envConfig.VnetName, envConfig.VnetSubnetName));

            //Setup shared sql db
            logger.LogInformation($"Setting up Shared SQL Database '{envConfig.SqlDbName}' on SQL Logical Server '{envConfig.SqlServerName}'.");
            await this.armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, new ArmSqlDb(envConfig.SqlServerName, envConfig.SqlDbName));
        }
    }
}
