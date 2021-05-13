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
    record LoginAcrController 
    (
        EnvironmentConfiguration config, 
        IAcrManager acrManager, 
        IShellRunner shellRunner,
        ILogger<LoginAcrController> logger
    )
    : IController
    {
        public async Task Run()
        {
            logger.LogInformation($"Logging into ACR '{config.AcrName}'.");

            var acrCreds = await acrManager.GetAcrCredential(config.AcrName, config.ResourceGroup);

            var passwordPath = Path.GetTempFileName();

            try
            {
                using (var passwordStream = new StreamWriter(File.Open(passwordPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
                {
                    passwordStream.Write(acrCreds.Password);
                }

                var acrLoginUrl = $"{config.AcrName}.azurecr.io";

                shellRunner.RunProcessVoid($"cat {passwordPath} | docker login {acrLoginUrl} --username {acrCreds.Username} --password-stdin", invalidExitCodeMessage: $"Error logging into ACR '{config.AcrName}'.");
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
