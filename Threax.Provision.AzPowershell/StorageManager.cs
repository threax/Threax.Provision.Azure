using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.Provision.AzPowershell
{
    public class StorageManager : IStorageManager
    {
        private readonly IShellRunner shellRunner;

        public StorageManager(IShellRunner shellRunner)
        {
            this.shellRunner = shellRunner;
        }

        public async Task<String> GetAccessKey(String AccountName, String ResourceGroupName)
        {
            var pwsh = shellRunner.CreateCommandBuilder();

            pwsh.SetUnrestrictedExecution();
            pwsh.AddCommand($"Import-Module Az.Storage");
            pwsh.AddResultCommand($"Get-AzStorageAccountKey -AccountName {AccountName} -ResourceGroupName {ResourceGroupName} | ConvertTo-Json -Depth 10");

            var error = $"Error getting storage account key for '{AccountName}' in Resource Group '{ResourceGroupName}'.";

            var result = await shellRunner.RunProcessAsync<List<AccessKeyInfo>>(pwsh,
                invalidExitCodeMessage: error);

            var key1 = result.First();

            return key1.Value ?? throw new InvalidOperationException(error);
        }
        class AccessKeyInfo
        {
            public string Value { get; set; }
        }
    }

}
