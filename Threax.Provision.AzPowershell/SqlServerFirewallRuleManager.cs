using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Threax.Provision.AzPowershell
{
    public class SqlServerFirewallRuleManager : IDisposable, ISqlServerFirewallRuleManager
    {
        private readonly ISqlServerManager sqlServerManager;
        private List<FirewallRuleInfo> createdRules = new List<FirewallRuleInfo>();
        private SemaphoreSlim ruleLock = new SemaphoreSlim(1, 1);

        public SqlServerFirewallRuleManager(ISqlServerManager sqlServerManager)
        {
            this.sqlServerManager = sqlServerManager;
        }

        public async Task Unlock(String serverName, String resourceGroupName, String startIp, String endIp)
        {
            var ruleName = Guid.NewGuid().ToString();

            try
            {
                await ruleLock.WaitAsync();
                //If this rule is already created, skip it
                if (createdRules.Any(i => i.ServerName == serverName && i.ResourceGroupName == resourceGroupName && i.StartIp == startIp && i.EndIp == endIp))
                {
                    return;
                }

                this.createdRules.Add(new FirewallRuleInfo(serverName, resourceGroupName, ruleName, startIp, endIp));
            }
            finally
            {
                ruleLock.Release();
            }

            await sqlServerManager.SetFirewallRule(ruleName, serverName, resourceGroupName, startIp, endIp);
        }

        public void Dispose()
        {
            ruleLock.Dispose();
            var ruleTasks = createdRules.Select(i => Task.Run(() => this.sqlServerManager.RemoveFirewallRule(i.RuleName, i.ServerName, i.ResourceGroupName))).ToList();
            foreach (var task in ruleTasks)
            {
                task.GetAwaiter().GetResult();
            }
        }

        class FirewallRuleInfo
        {
            public FirewallRuleInfo(string serverName, string resourceGroupName, string ruleName, string startIp, string endIp)
            {
                ServerName = serverName;
                ResourceGroupName = resourceGroupName;
                RuleName = ruleName;
                StartIp = startIp;
                EndIp = endIp;
            }

            public String ServerName { get; set; }

            public String ResourceGroupName { get; set; }

            public String RuleName { get; set; }

            public String StartIp { get; set; }

            public String EndIp { get; set; }
        }
    }
}
