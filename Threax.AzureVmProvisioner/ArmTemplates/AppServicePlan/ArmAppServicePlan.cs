using System;
using System.Collections.Generic;
using System.Text;
using Threax.Provision.AzPowershell;
using Threax.Provision.Azure.Core;

namespace Threax.AzureVmProvisioner.ArmTemplates.AppServicePlan
{
    class ArmAppServicePlan : ArmTemplate
    {
        public ArmAppServicePlan(String name)
        {
            this.nameFromTemplate = name;
        }

        public String nameFromTemplate { get; set; }
    }
}
