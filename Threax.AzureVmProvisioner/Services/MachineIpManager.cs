using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    public class MachineIpManager : IMachineIpManager
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<MachineIpManager> logger;
        private String ip;

        public MachineIpManager(HttpClient httpClient, ILogger<MachineIpManager> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<String> GetExternalIp()
        {
            if (ip == null)
            {
                var ipInfoHost = "http://ipinfo.io/json";
                using (var result = await httpClient.GetAsync(ipInfoHost))
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"Could not get public ip from '{ipInfoHost}'.");
                    }
                    var json = await result.Content.ReadAsStringAsync();
                    var jobj = JObject.Parse(json);
                    ip = jobj["ip"]?.ToString();
                }
            }

            return ip;
        }
    }
}
