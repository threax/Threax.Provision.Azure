using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    record AppSecretCreator
    (
        RandomNumberGenerator RandomNumberGenerator
    ) : IAppSecretCreator
    {
        public String CreateSecret()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
