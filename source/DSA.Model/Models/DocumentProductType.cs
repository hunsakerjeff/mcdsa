using System.Collections.Generic;

namespace DSA.Model.Models
{
    public class DocumentProductType
    {
        public string ProductType { get; set; }

        public IList<string> DocumentIds { get; set; } = new List<string>();
    }
}
