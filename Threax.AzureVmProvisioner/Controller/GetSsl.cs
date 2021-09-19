using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
        IKeyVaultManager KeyVaultManager,
        IOSHandler OSHandler,
        ILogger<GetSsl> Logger
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
                //docker run -it --rm -v {certTempPath}:/etc/letsencrypt certbot/certbot
                var authHookPath = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Services", "WatchCert.sh");
                var authHookTempPath = Path.Combine(certTempPath, "auth-hook.sh");
                var script = File.ReadAllText(authHookPath);
                var certInfoFile = Path.Combine(certTempPath, "CertInfo.json");
                script = script.Replace("REPLACE_OUT_FILE", certInfoFile);
                File.WriteAllText(authHookTempPath, script);
                OSHandler.MakeExecutable(authHookTempPath);
                
                //"--staging --server https://acme-staging-v02.api.letsencrypt.org/directory";
                var certTask = ShellRunner.RunProcessVoidAsync($"certbot certonly --server https://acme-v02.api.letsencrypt.org/directory --manual --config-dir {certTempPath} --manual-auth-hook {authHookTempPath} --preferred-challenges dns --agree-tos --manual-public-ip-logging-ok --no-eff-email --email {email} -d {commonName}");

                var timeout = 10;
                while (!File.Exists(certInfoFile))
                {
                    await Task.Delay(300);
                    if(--timeout < 0)
                    {
                        throw new InvalidOperationException($"Could not find file '{certInfoFile}' that the hook script should have created, but didn't.");
                    }
                }

                var json = File.ReadAllText(certInfoFile);
                dynamic data = JObject.Parse(json);

                Logger.LogInformation("Create a txt record in your dns.");
                Logger.LogInformation($"Certificate lookup url is '{data.url}'");
                Logger.LogInformation($"Certificate validation '{data.validation}'");
                Logger.LogInformation("Please wait, this can potentially take a little while. There is a log file called watch-log that can be viewed.");

                await certTask;

                var finalCertPath = Path.Combine(certTempPath, "archive", baseUrl);
                var publicKeyPath = Path.Combine(finalCertPath, "fullchain1.pem");
                var privateKeyPath = Path.Combine(finalCertPath, "privkey1.pem");

                var publicKey = File.ReadAllText(publicKeyPath);
                var privateKey = File.ReadAllText(privateKeyPath);

                Logger.LogInformation($"Copying public key '{envConfig.SslPublicKeyName}' and private key '{envConfig.SslPrivateKeyName}' to '{envConfig.ExternalKeyVaultName}'");

                //Copy certs to key vaults
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
