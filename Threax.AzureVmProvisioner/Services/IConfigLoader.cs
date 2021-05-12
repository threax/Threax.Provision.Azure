using Newtonsoft.Json.Linq;

namespace Threax.AzureVmProvisioner.Services
{
    interface IConfigLoader
    {
        /// <summary>
        /// A JObject with the config loaded once. This is not suitable to modify since it will be shared with other components.
        /// </summary>
        public JObject SharedConfigInstance { get; }

        /// <summary>
        /// Load the config. You will get a fresh JObject each time this is called, so the result is safe to modify.
        /// </summary>
        /// <returns></returns>
        JObject LoadConfig();
    }
}