using System;
using Threax.ProcessHelper.Pwsh;
using Threax.Provision.AzCli.Models;

namespace Threax.Provision.AzCli.Managers
{
    class AccountManager : IAccountManager
    {
        private readonly IPowershellCoreRunner<VerboseRunnerType> powershellCoreRunner;

        public AccountManager(IPowershellCoreRunner<VerboseRunnerType> powershellCoreRunner)
        {
            this.powershellCoreRunner = powershellCoreRunner;
        }

        public void SetSubscription(String nameOrId)
        {
            powershellCoreRunner.RunProcessVoid($"az account set --subscription {nameOrId}", 0, $"Error setting current subscription to '{nameOrId}'.");
        }

        public AccountShowOutput? Show()
        {
            return powershellCoreRunner.RunProcess<AccountShowOutput>($"az account show", 0, "Error getting account info.");
        }
    }
}
