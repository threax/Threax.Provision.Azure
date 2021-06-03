using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.ConsoleApp;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ILogin : IController
    {
        Task Run(Configuration configuration);
    }

    [HelpInfo(HelpCategory.Primary, "Log into the Azure Services")]
    record Login
    (
        IShellRunner shellRunner,
        ILogger<Login> logger
    )
    : ILogin
    {
        public async Task Run(Configuration configuration)
        {
            //Az Powershell
            logger.LogInformation("Log into Az Powershell");
            await shellRunner.RunProcessVoidAsync($"Connect-AzAccount -UseDeviceAuthentication", invalidExitCodeMessage: $"Error running Connect-AzAccount.");

            //VSTeam
            var account = configuration.Environment.AzDoAccount;
            if (account != null)
            {
                logger.LogInformation("Log into VSTeam");
                logger.LogInformation("Please provide the access token.");
                var accessToken = SecureConsole.ReadLineMasked();
                await shellRunner.RunProcessVoidAsync($"Add-VSTeamProfile -Account {account} -Name threaxprovisioner -PersonalAccessToken {accessToken} -Version VSTS", invalidExitCodeMessage: $"Error logging into VSTeam.");
            }
            else
            {
                logger.LogWarning($"You must include a '{nameof(configuration.Environment)}.{nameof(configuration.Environment.AzDoAccount)}' property in your environment config to use azure devops commands.");
            }
        }
    }
}
