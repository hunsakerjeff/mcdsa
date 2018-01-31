using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "EntitySubscription")]
    public class EntitySubscription
    {
        public string Id { get; set; }

        public string ParentId { get; set; }
    }

}
