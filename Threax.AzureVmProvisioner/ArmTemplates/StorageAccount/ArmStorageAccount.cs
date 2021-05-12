﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Threax.Provision.AzPowershell;
using Threax.Provision.Azure.Core;

namespace Threax.AzureVmProvisioner.ArmTemplates.StorageAccount
{
    class ArmStorageAccount : ArmTemplate
    {
        public ArmStorageAccount(String storageAccountName, String location)
        {
            this.storageAccountName = storageAccountName;
            this.location = location;
        }

        public String location { get; set; }

        public String storageAccountName { get; set; }
    }
}
