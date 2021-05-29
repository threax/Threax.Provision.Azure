using System;
using System.Threading;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IFullDeploy : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Primary, "Run a complete deployment including create common on the specified files.")]
    record FullDeploy
    (
        ICreateCommon CreateCommon,
        ILoginAcr LoginAcr,
        IManageApp ManageApp
    ) : IFullDeploy, IDisposable
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private bool needsCommon = true;

        public void Dispose()
        {
            semaphore.Dispose();
        }

        public async Task Run(Configuration config)
        {
            try
            {
                //This will cause everything to wait until the common stuff is done
                await semaphore.WaitAsync();
                if (needsCommon)
                {
                    needsCommon = false;
                    await CreateCommon.Run(config);
                    await LoginAcr.Run(config);
                }
            }
            finally
            {
                semaphore.Release();
            }

            await ManageApp.Run(config);
        }
    }
}
