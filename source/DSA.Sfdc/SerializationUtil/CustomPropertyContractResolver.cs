using System;
using System.Collections.Generic;
using DSA.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DSA.Sfdc.SerializationUtil
{
    class CustomPropertyContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var prefix = SfdcConfig.CustomerPrefix;
            IList<JsonProperty> list = base.CreateProperties(type, memberSerialization);
            
            foreach (var prop in list)
            {
                prop.PropertyName = string.Format(prop.PropertyName, prefix);
            }

            return list;
        }
    }
}
