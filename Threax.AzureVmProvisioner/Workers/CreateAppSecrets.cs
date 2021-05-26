using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateAppSecrets
    (
        EnvironmentConfiguration config,
        ResourceConfiguration resourceConfiguration,
        ILogger<CreateAppSecrets> logger,
        IAppSecretCreator AppSecretCreator,
        IKeyVaultManager keyVaultManager,
        AzureKeyVaultConfig azureKeyVaultConfig,
        IVmCommands vmCommands,
        ISshCredsManager sshCredsManager,
        DeploymentConfig DeploymentConfig
    )
    : IWorker<CreateAppSecrets>
    {
        public async Task ExecuteAsync()
        {
            var serverSideFilesToRemove = new List<String>();
            try
            {
                var idReg = resourceConfiguration.IdServerRegistration;
                if (idReg != null)
                {
                    logger.LogInformation($"Configuring id server secrets for '{resourceConfiguration.Compute.Name}' in vault '{azureKeyVaultConfig.VaultName}' and in id server.");
                    await keyVaultManager.UnlockSecrets(azureKeyVaultConfig.VaultName, config.UserId);
                    switch (idReg.Type)
                    {
                        case IdServerRegistrationType.None:
                            break;
                        case IdServerRegistrationType.AppDashboard:
                            await SetupAppDashboard(serverSideFilesToRemove);
                            break;
                        case IdServerRegistrationType.RegularApp:
                            
                            break;
                        default:
                            throw new InvalidOperationException($"{nameof(IdServerRegistrationType)} '{idReg.Type}' not supported.");
                    }
                }
            }
            finally
            {
                //Remove server side files
                foreach (var file in serverSideFilesToRemove)
                {
                    try
                    {
                        await sshCredsManager.RunSshCommand($"rm -f \"{file}\"");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"{ex.GetType().Name} erasing server side file '{file}'. Message: '{ex.Message}'. Please remove file manually.");
                    }
                }
            }
        }

        private async Task SetupAppDashboard(List<String> serverSideFilesToRemove)
        {
            logger.LogInformation("Create jwt secret");
            var idReg = resourceConfiguration.IdServerRegistration;
            var jwtAuth = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);
            var serverJwtAuthPath = $"~/{Guid.NewGuid()}";
            serverSideFilesToRemove.Add(serverJwtAuthPath);
            await sshCredsManager.CopyStringToSshFile(jwtAuth, serverJwtAuthPath);

            logger.LogInformation("Register with id server");
            await vmCommands.ThreaxDockerToolsExec($"/app/{idReg.IdServerPath}", "SetupAppDashboard", new List<string>() { 
                "--exec-load", "File", "jwtAuthSecret", serverJwtAuthPath 
            });
        }

        private async Task SetupRegularApp(List<String> serverSideFilesToRemove)
        {
            var idReg = resourceConfiguration.IdServerRegistration;

            logger.LogInformation("Create jwt secret");
            var jwtAuth = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName, jwtAuth);
            var serverJwtAuthPath = $"~/{Guid.NewGuid()}";
            serverSideFilesToRemove.Add(serverJwtAuthPath);
            await sshCredsManager.CopyStringToSshFile(jwtAuth, serverJwtAuthPath);

            logger.LogInformation("Create client creds secret");
            var sharedClientCreds = AppSecretCreator.CreateSecret();
            await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, idReg.ClientCredentialsSecretName, sharedClientCreds);
            var serverClientCredsPath = $"~/{Guid.NewGuid()}";
            serverSideFilesToRemove.Add(serverClientCredsPath);
            await sshCredsManager.CopyStringToSshFile(sharedClientCreds, serverClientCredsPath);

            logger.LogInformation("Register with id server");
            await vmCommands.ThreaxDockerToolsExec($"/app/{idReg.IdServerPath}", "AddFromMetadata", new List<string>() {
                $"https://{DeploymentConfig.Name}.{config.BaseUrl}",
                "--exec-load", "File", "jwtAuthSecret", serverJwtAuthPath, 
                "--exec-load", "File", "clientCredsSecret", serverClientCredsPath 
            });
        }
    }
}
