using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppSqlDatabase : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Create, "Create the SQL database for the current app. This registers the users in the shared db compute.")]
    record CreateAppSqlDatabase
    (
        ISqlServerManager sqlServerManager,
        IKeyVaultManager keyVaultManager,
        ICredentialLookup credentialLookup,
        ISqlServerFirewallRuleManager sqlServerFirewallRuleManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ILogger<CreateAppSqlDatabase> logger,
        IMachineIpManager machineIpManager
    )
    : ICreateAppSqlDatabase
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var resources = config.Resources;
            var azureKeyVaultConfig = config.KeyVault;

            var resource = resources.SqlDatabase;

            if(resource == null)
            {
                return;
            }

            logger.LogInformation($"Processing sql database credentials for '{resource.Name}'");

            var readerKeyBase = $"threaxpipe-{resource.Name}-readwrite" ?? throw new InvalidOperationException($"You must include a '{nameof(SqlDatabase.Name)}' property on '{nameof(SqlDatabase)}' types.");
            var ownerKeyBase = $"threaxpipe-{resource.Name}-owner";

            //In this setup there is actually only 1 db to save money.
            //So both the sql server and the db will be provisioned in this step.
            //You would want to have separate dbs in a larger setup.
            await keyVaultAccessManager.Unlock(envConfig.InfraKeyVaultName, envConfig.UserId);
            await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, envConfig.UserId);
            var machineIp = await machineIpManager.GetExternalIp();
            await sqlServerFirewallRuleManager.Unlock(envConfig.SqlServerName, envConfig.ResourceGroup, machineIp, machineIp);

            var saCreds = await credentialLookup.GetOrCreateCredentials(envConfig.InfraKeyVaultName, envConfig.SqlSaBaseKey);
            var saConnectionString = sqlServerManager.CreateConnectionString(envConfig.SqlServerName, envConfig.SqlDbName, saCreds.User, saCreds.Pass);

            //Setup user in new db
            logger.LogInformation($"Setting up users for {resource.Name} in Shared SQL Database '{envConfig.SqlDbName}' on SQL Logical Server '{envConfig.SqlServerName}'.");
            var dbContext = new ProvisionDbContext(saConnectionString);
            var readWriteCreds = await credentialLookup.GetOrCreateCredentials(azureKeyVaultConfig.VaultName, readerKeyBase);
            var ownerCreds = await credentialLookup.GetOrCreateCredentials(azureKeyVaultConfig.VaultName, ownerKeyBase);
            int result;
            //This isn't great, but just ignore this exception for now. If the user isn't created the lines below will fail.
            try
            {
                result = await dbContext.Database.ExecuteSqlRawAsync($"CREATE USER {readWriteCreds.User} WITH PASSWORD = '{readWriteCreds.Pass}';");
            }
            catch (SqlException) { }
            try
            {
                result = await dbContext.Database.ExecuteSqlRawAsync($"CREATE USER {ownerCreds.User} WITH PASSWORD = '{ownerCreds.Pass}';");
            }
            catch (SqlException) { }
            result = await dbContext.Database.ExecuteSqlRawAsync($"ALTER USER {readWriteCreds.User} WITH PASSWORD = '{readWriteCreds.Pass}';");
            result = await dbContext.Database.ExecuteSqlRawAsync($"ALTER USER {ownerCreds.User} WITH PASSWORD = '{ownerCreds.Pass}';");
            result = await dbContext.Database.ExecuteSqlRawAsync($"ALTER ROLE db_datareader ADD MEMBER {readWriteCreds.User}");
            result = await dbContext.Database.ExecuteSqlRawAsync($"ALTER ROLE db_datawriter ADD MEMBER {readWriteCreds.User}");
            result = await dbContext.Database.ExecuteSqlRawAsync($"ALTER ROLE db_owner ADD MEMBER {ownerCreds.User}");

            //Always set the main connection string
            logger.LogInformation($"Setting app db connection string '{resource.ConnectionStringName}'");
            var appConnectionString = sqlServerManager.CreateConnectionString(envConfig.SqlServerName, envConfig.SqlDbName, readWriteCreds.User, readWriteCreds.Pass);
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, resource.ConnectionStringName, appConnectionString);

            if (!String.IsNullOrEmpty(resource.OwnerConnectionStringName))
            {
                logger.LogInformation($"Setting owner db connection string '{resource.OwnerConnectionStringName}'");
                var ownerConnectionString = sqlServerManager.CreateConnectionString(envConfig.SqlServerName, envConfig.SqlDbName, ownerCreds.User, ownerCreds.Pass);
                await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, resource.OwnerConnectionStringName, ownerConnectionString);
            }
        }
    }
}
