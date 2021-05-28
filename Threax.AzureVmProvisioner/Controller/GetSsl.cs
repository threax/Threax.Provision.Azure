using System;
using System.IO;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Services;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IGetSsl : IController
    {
        Task Run(Configuration configuration);
    }

    [HelpInfo(HelpCategory.Primary, "Get a SSL certificate from certbot and store it in the external key vault.")]
    record GetSsl
    (
        IPathHelper PathHelper,
        IShellRunner ShellRunner,
        IKeyVaultManager KeyVaultManager
    ) 
    : IGetSsl
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;

            if (!await KeyVaultManager.Exists(envConfig.ExternalKeyVaultName))
            {
                throw new InvalidOperationException($"You must create a key vault named '{envConfig.ExternalKeyVaultName}' before getting a ssl cert. CreateCommon will do this for you.");
            }

            var keyVaultUnlockTask = KeyVaultManager.UnlockSecrets(envConfig.ExternalKeyVaultName, envConfig.UserId);

            var baseUrl = envConfig.BaseUrl ?? throw new InvalidOperationException($"You must include a '{nameof(envConfig.BaseUrl)}' property when making ssl certificates.");
            var commonName = $"*.{baseUrl}";
            var email = envConfig.SslEmail ?? throw new InvalidOperationException($"You must include a '{nameof(envConfig.SslEmail)}' property when making ssl certificates.");
            var path = config.GetConfigDirectory();

            var time = DateTime.Now.ToString("yyyyMMddhhmmss");
            var certTempPath = Path.GetFullPath(Path.Combine(path, ".files", $"cert_{time}"));
            if (!Directory.Exists(certTempPath))
            {
                Directory.CreateDirectory(certTempPath);
            }

            try
            {
                //Get cert with certbot container
                ShellRunner.RunProcessVoid($"docker run -it --rm -v {certTempPath}:/etc/letsencrypt certbot/certbot certonly --manual --server https://acme-v02.api.letsencrypt.org/directory --preferred-challenges dns --agree-tos --manual-public-ip-logging-ok --no-eff-email --email {email} -d {commonName}");

                var finalCertPath = Path.Combine(certTempPath, "archive", baseUrl);
                var publicKeyPath = Path.Combine(finalCertPath, "fullchain1.pem");
                var privateKeyPath = Path.Combine(finalCertPath, "privkey1.pem");

                var publicKey = File.ReadAllText(publicKeyPath);
                var privateKey = File.ReadAllText(privateKeyPath);

                //Copy certs to key vaults
                await keyVaultUnlockTask;
                await Task.WhenAll
                (
                    KeyVaultManager.SetSecret(envConfig.ExternalKeyVaultName, envConfig.SslPublicKeyName, publicKey),
                    KeyVaultManager.SetSecret(envConfig.ExternalKeyVaultName, envConfig.SslPrivateKeyName, privateKey)
                );
            }
            finally
            {
                if (Directory.Exists(certTempPath))
                {
                    Directory.Delete(certTempPath, true);
                }
            }
        }
    }
}
