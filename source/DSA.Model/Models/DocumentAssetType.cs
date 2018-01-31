using System.Collections.Generic;

namespace DSA.Model.Models
{
    public class DocumentAssetType
    {
        public string AssetType { get; set; }

        public IList<string> DocumentIds { get; set; } = new List<string>();
    }
}
