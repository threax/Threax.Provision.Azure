using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Threax.Provision.AzPowershell
{
    public class KeyVaultAccessManager : IDisposable, IKeyVaultAccessManager
    {
        private readonly IKeyVaultManager keyVaultManager;
        private List<KeyVaultRuleInfo> createdRules = new List<KeyVaultRuleInfo>();
        private SemaphoreSlim ruleLock = new SemaphoreSlim(1, 1);

        public KeyVaultAccessManager(IKeyVaultManager keyVaultManager)
        {
            this.keyVaultManager = keyVaultManager;
        }

        public async Task Unlock(String keyVaultName, Guid userId)
        {
            try
            {
                await ruleLock.WaitAsync();

                //If this user is already unlocked, do nothing
                if (createdRules.Any(i => i.KeyVaultName == keyVaultName && i.UserId == userId))
                {
                    return;
                }
                await keyVaultManager.UnlockSecrets(keyVaultName, userId);
                this.createdRules.Add(new KeyVaultRuleInfo(keyVaultName, userId));
            }
            finally
            {
                ruleLock.Release();
            }
        }

        public void Dispose()
        {
            ruleLock.Dispose();
            //Just leave access as is, uncomment to remove when program shuts down
            //var ruleTasks = createdRules.Select(i => Task.Run(() => this.keyVaultManager.LockSecrets(i.KeyVaultName, i.UserId))).ToList();
            //foreach (var task in ruleTasks)
            //{
            //    task.GetAwaiter().GetResult();
            //}
        }

        class KeyVaultRuleInfo
        {
            public KeyVaultRuleInfo(string keyVaultName, Guid userId)
            {
                KeyVaultName = keyVaultName;
                UserId = userId;
            }

            public string KeyVaultName { get; set; }
            public Guid UserId { get; set; }
        }
    }
}
