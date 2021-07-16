using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ILoadExternalSecretsWorker : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Create, "Load secrets from the external secrets vault for the given app.")]
    record LoadExternalSecretsWorker
    (
        IKeyVaultManager KeyVaultManager,
        IVmCommands VmCommands,
        ILogger<LoadExternalSecretsWorker> Logger
    ) 
        : ILoadExternalSecretsWorker
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var resources = config.Resources;
            var azureKeyVaultConfig = config.KeyVault;

            if (resources.ExternalSecrets == null)
            {
                return;
            }

            Logger.LogInformation($"Managing external secrets.");

            foreach (var externalSecret in resources.ExternalSecrets)
            {
                Logger.LogInformation($"Copying secret '{externalSecret.Source}' to '{externalSecret.Destination}' of type '{externalSecret.Type}'");

                var secret = await KeyVaultManager.GetSecret(envConfig.ExternalKeyVaultName, externalSecret.Source);
                switch (externalSecret.Type)
                {
                    case ExternalSecretDestinationType.AppKeyVault:
                        if (azureKeyVaultConfig?.VaultName == null)
                        {
                            throw new InvalidOperationException($"You must define a '{nameof(azureKeyVaultConfig.VaultName)}' property in your KeyVault section to store external secrets in a key vault.");
                        }
                        await KeyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, externalSecret.Destination, secret);
                        break;
                    case ExternalSecretDestinationType.Local:
                        var fileName = Path.GetFileName(config.GetConfigPath());
                        var serverConfigFilePath = $"/app/{resources.Compute.Name}/{fileName}";
                        await VmCommands.SetSecretFromString(envConfig.VmName, envConfig.ResourceGroup, config.GetConfigPath(), serverConfigFilePath, externalSecret.Destination, secret);
                        break;
                    default:
                        throw new InvalidOperationException($"Destination type '{externalSecret.Destination}' is not supported for loading external secrets.");
                }
            }
        }
    }
}
