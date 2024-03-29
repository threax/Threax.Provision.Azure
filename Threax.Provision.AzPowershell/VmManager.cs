﻿using System;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.Provision.AzPowershell
{
    public class VmManager : IVmManager
    {
        private readonly IShellRunner shellRunner;

        public VmManager(IShellRunner shellRunner)
        {
            this.shellRunner = shellRunner;
        }

        public async Task<String> GetPublicIp(String Name)
        {
            var pwsh = shellRunner.CreateCommandBuilder();

            pwsh.SetUnrestrictedExecution();
            pwsh.AddCommand($"Import-Module Az.Network");
            pwsh.AddResultCommand($"Get-AzPublicIpAddress -Name {Name} | ConvertTo-Json -Depth 10");

            dynamic result = await shellRunner.RunProcessAsync(pwsh,
               invalidExitCodeMessage: $"Error getting Public Ip Address '{Name}'.");

            return result.IpAddress;
        }

        public Task SetSecurityRuleAccess(String NetworkSecurityGroup, String ResourceGroup, String Name, String Access, String SourceAddressPrefix)
        {
            {
                var pwsh = shellRunner.CreateCommandBuilder();

                //Workaround from spaelling https://github.com/Azure/azure-powershell/issues/8371#issuecomment-512549409
                pwsh.SetUnrestrictedExecution();
                pwsh.AddCommand($"Import-Module Az.Network");
                pwsh.AddCommand($"$sourceAddrs = New-Object System.Collections.Generic.List[string]");
                pwsh.AddCommand($"$sourceAddrs.Add({SourceAddressPrefix})");
                pwsh.AddCommand($"$nsg = Get-AzNetworkSecurityGroup -Name {NetworkSecurityGroup} -ResourceGroup {ResourceGroup}");
                pwsh.AddCommand($"($nsg.SecurityRules | Where-Object {{$_.Name -eq {Name}}}).Access = {Access}");
                pwsh.AddCommand($"($nsg.SecurityRules | Where-Object {{$_.Name -eq {Name}}}).SourceAddressPrefix = $sourceAddrs");
                pwsh.AddResultCommand($"$nsg | Set-AzNetworkSecurityGroup | ConvertTo-Json -Depth 10");

                return shellRunner.RunProcessVoidAsync(pwsh,
                    invalidExitCodeMessage: $"Error modifying NSG '{NetworkSecurityGroup}' from '{ResourceGroup}'.");
            }
        }
    }
}
