using System;
using Newtonsoft.Json;

namespace DSA.Sfdc.SerializationUtil
{
    public class CustomJsonSerializerSettings
    {
        private static readonly Lazy<CustomJsonSerializerSettings> Lazy = new Lazy<CustomJsonSerializerSettings>(() => new CustomJsonSerializerSettings());

        public static CustomJsonSerializerSettings Instance => Lazy.Value;

        private CustomJsonSerializerSettings()
        {
            Settings = new JsonSerializerSettings
            {
                ContractResolver = new CustomPropertyContractResolver()
            };
        }

        public JsonSerializerSettings Settings { get; }
    }
}
