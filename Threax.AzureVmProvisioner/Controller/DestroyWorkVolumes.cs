﻿using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Controller
{
    interface IDestroyWorkVolumes : IController
    {
        Task Run();
    }

    [HelpInfo(HelpCategory.Primary, "Destroy the work volumes used for running the provisioner in a container.")]
    record DestroyWorkVolumes
    (
        ILogger<DestroyWorkVolumes> logger, 
        IShellRunner shellRunner
    ) : IDestroyWorkVolumes
    {
        public async Task Run()
        {
            await shellRunner.RunProcessVoidAsync($"docker volume remove threax-provision-azurevm-home", "An error occured removing home volume.");
            await shellRunner.RunProcessVoidAsync($"docker volume remove threax-provision-azurevm-temp", "An error occured removing temp volume.");
        }
    }
}
