using Newtonsoft.Json;

namespace DSA.Sfdc.SerializationUtil
{
    public static class CustomPrefixJsonConvert
    {
        public static T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, CustomJsonSerializerSettings.Instance.Settings);
        }

        public static string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value, CustomJsonSerializerSettings.Instance.Settings);
        }
    }
}
