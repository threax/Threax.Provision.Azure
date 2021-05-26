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
    record RegisterIdServer
    (
        EnvironmentConfiguration config,
        ResourceConfiguration resourceConfiguration,
        ILogger<RegisterIdServer> logger,
        IAppSecretCreator AppSecretCreator,
        IKeyVaultManager keyVaultManager,
        AzureKeyVaultConfig azureKeyVaultConfig,
        IVmCommands vmCommands,
        ISshCredsManager sshCredsManager,
        DeploymentConfig DeploymentConfig
    )
    : IWorker<RegisterIdServer>
    {
        public async Task ExecuteAsync()
        {
            var serverSideFilesToRemove = new List<String>();
            try
            {
                var idReg = resourceConfiguration.IdServerRegistration;
                if (idReg != null)
                {
                    logger.LogInformation($"Registering app '{resourceConfiguration.Compute.Name}' in id server on path '{idReg.IdServerPath}'.");
                    await keyVaultManager.UnlockSecrets(azureKeyVaultConfig.VaultName, config.UserId);
                    switch (idReg.Type)
                    {
                        case IdServerRegistrationType.None:
                            break;
                        case IdServerRegistrationType.AppDashboard:
                            await SetupAppDashboard(serverSideFilesToRemove);
                            break;
                        case IdServerRegistrationType.RegularApp:
                            await SetupRegularApp(serverSideFilesToRemove);
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
            var idReg = resourceConfiguration.IdServerRegistration;
            logger.LogInformation($"Get jwt secret '{idReg.JwtAuthSecretName}' from '{azureKeyVaultConfig.VaultName}'");
            var jwtAuth = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName);
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

            logger.LogInformation($"Get jwt secret '{idReg.JwtAuthSecretName}' from '{azureKeyVaultConfig.VaultName}'");
            var jwtAuth = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, idReg.JwtAuthSecretName);
            var serverJwtAuthPath = $"~/{Guid.NewGuid()}";
            serverSideFilesToRemove.Add(serverJwtAuthPath);
            await sshCredsManager.CopyStringToSshFile(jwtAuth, serverJwtAuthPath);

            logger.LogInformation($"Get client creds secret '{idReg.ClientCredentialsSecretName}' from '{azureKeyVaultConfig.VaultName}'");
            var sharedClientCreds = await keyVaultManager.GetSecret(azureKeyVaultConfig.VaultName, idReg.ClientCredentialsSecretName);
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
