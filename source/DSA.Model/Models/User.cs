using DSA.Model.Util;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    public class User
    {
        public string Id { get; set; }

        [JsonIgnore]
        public string ProfileId {
            get
            {
                if (!string.IsNullOrWhiteSpace(StandardProfileId)) {
                    return StandardProfileId;
                }
                else if (!string.IsNullOrWhiteSpace(CustomProfileProfileId))
                {
                    return IdConverter.ConvertTo18(CustomProfileProfileId);
                }
                else
                {
                    return CustomProfileProfileId;
                }
            }
        }

        [JsonProperty("ProfileId")]
        public string StandardProfileId { get; set; }

        [JsonProperty("{0}__Custom_Profile_ID__c")]
        public string CustomProfileProfileId { get; set; }
    }

    public class NullUser : User
    {
        public NullUser()
        {
            Id = string.Empty;
            StandardProfileId = string.Empty;
            CustomProfileProfileId = string.Empty;
        }
    } 
}
