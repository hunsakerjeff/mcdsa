using System.Collections.Generic;

namespace DSA.Model.Models
{
    public class DocumentsTag
    {
        public string Tag { get; set; }

        public IList<string> DocumentIds { get; set; } = new List<string>();
    }
}
