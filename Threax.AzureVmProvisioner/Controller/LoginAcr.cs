using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ILoginAcr : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Log the machine running this app into the created ACR.")]
    record LoginAcr
    (
        IAcrManager acrManager, 
        IShellRunner shellRunner,
        ILogger<LoginAcr> logger
    )
    : ILoginAcr
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;

            logger.LogInformation($"Logging into ACR '{envConfig.AcrName}'.");

            var acrCreds = await acrManager.GetAcrCredential(envConfig.AcrName, envConfig.ResourceGroup);

            var passwordPath = Path.GetTempFileName();

            try
            {
                using (var passwordStream = new StreamWriter(File.Open(passwordPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
                {
                    passwordStream.Write(acrCreds.Password);
                }

                var acrLoginUrl = $"{envConfig.AcrName}.azurecr.io";

                shellRunner.RunProcessVoid($"cat {passwordPath} | docker login {acrLoginUrl} --username {acrCreds.Username} --password-stdin", invalidExitCodeMessage: $"Error logging into ACR '{envConfig.AcrName}'.");
            }
            finally
            {
                //Erase the file
                if (File.Exists(passwordPath))
                {
                    File.Delete(passwordPath);
                }
            }
        }
    }
}
