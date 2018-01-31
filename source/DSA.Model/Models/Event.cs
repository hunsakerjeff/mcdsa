using System;

namespace DSA.Model.Models
{
    public class Event
    {
        public string Description { get; set; }
        
        public string Subject { get; set; }
        
        public DateTime ActivityDateTime { get; set; }
        
        public int DurationInMinutes { get; set; }

        public string WhoId { get; set; }

    }
}
