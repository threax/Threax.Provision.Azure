using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services.CreateCommon;

namespace Threax.AzureVmProvisioner.Controller
{
    class CreateCommonController : IController
    {
        private readonly CreateCommonCompute compute;
        private readonly CreateCommonKeyVault keyVault;
        private readonly CreateCommonResourceGroup resourceGroup;
        private readonly CreateCommonSqlDatabase sqlDatabase;
        private readonly ILogger<CreateCommonController> logger;

        public CreateCommonController
            (
            CreateCommonCompute compute,
            CreateCommonKeyVault keyVault,
            CreateCommonResourceGroup resourceGroup,
            CreateCommonSqlDatabase sqlDatabase,
            ILogger<CreateCommonController> logger
            )
        {
            this.compute = compute;
            this.keyVault = keyVault;
            this.resourceGroup = resourceGroup;
            this.sqlDatabase = sqlDatabase;
            this.logger = logger;
        }

        public async Task Run()
        {
            logger.LogInformation("Creating common resources.");
            await this.resourceGroup.Execute();
            await this.keyVault.Execute();
            await this.compute.Execute();
            await this.sqlDatabase.Execute();
        }
    }
}
