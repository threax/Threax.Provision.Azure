﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Threax.AzureVmProvisioner.ArmTemplates.SqlDb;
using Threax.AzureVmProvisioner.ArmTemplates.SqlServer;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using System;
using System.Threading.Tasks;
using Threax.Provision;
using Threax.Provision.AzPowershell;
using Microsoft.Extensions.Logging;

namespace Threax.AzureVmProvisioner.Controller.CreateCommon
{
    class CreateCommonSqlDatabase
    {
        private readonly EnvironmentConfiguration config;
        private readonly ICredentialLookup credentialLookup;
        private readonly IArmTemplateManager armTemplateManager;
        private readonly IKeyVaultAccessManager keyVaultAccessManager;
        private readonly ILogger<CreateCommonSqlDatabase> logger;

        public CreateCommonSqlDatabase(
            EnvironmentConfiguration config, 
            ICredentialLookup credentialLookup,
            IArmTemplateManager armTemplateManager,
            IKeyVaultAccessManager keyVaultAccessManager,
            ILogger<CreateCommonSqlDatabase> logger)
        {
            this.config = config;
            this.credentialLookup = credentialLookup;
            this.armTemplateManager = armTemplateManager;
            this.keyVaultAccessManager = keyVaultAccessManager;
            this.logger = logger;
        }

        public async Task Execute()
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
