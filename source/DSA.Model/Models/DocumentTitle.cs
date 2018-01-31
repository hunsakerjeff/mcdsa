using System.Collections.Generic;

namespace DSA.Model.Models
{
    public class DocumentTitle
    {
        public string Title { get; set; }

        public IList<string> DocumentIds { get; set; } = new List<string>();
    }
}
