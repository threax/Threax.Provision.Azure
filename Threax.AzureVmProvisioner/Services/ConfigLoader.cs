using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    class ConfigLoader : IConfigLoader
    {
        private const String IncludeKey = "$include";
        private readonly string path;
        private Lazy<JObject> config;

        public ConfigLoader(string path)
        {
            this.path = path;
            config = new Lazy<JObject>(() => LoadConfig());
        }

        public JObject LoadConfig()
        {
            return LoadWithInclude(path);
        }

        public JObject SharedConfigInstance => config.Value;

        private static JObject LoadWithInclude(String path)
        {
            var fullPath = Path.GetFullPath(path);
            var rootFolder = Path.GetDirectoryName(fullPath);
            //Load the file to see if there are any includes
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Cannot find include file {path}.\nFullPath: {fullPath}", fullPath);
            }

            JObject jObj;
            using (var reader = new StreamReader(File.OpenRead(fullPath)))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    jObj = JToken.ReadFrom(jsonReader) as JObject;
                }
            }

            var include = jObj[IncludeKey];
            if(include != null)
            {
                var includePath = Path.Combine(rootFolder, include.Value<String>());
                var parent = LoadWithInclude(includePath);
                parent.Merge(jObj, new JsonMergeSettings()
                {
                    MergeArrayHandling = MergeArrayHandling.Concat,
                    MergeNullValueHandling = MergeNullValueHandling.Merge,
                    PropertyNameComparison = StringComparison.Ordinal
                });
                jObj = parent;
            }

            return jObj;
        }
    }
}
