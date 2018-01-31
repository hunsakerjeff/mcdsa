using System.Collections.Generic;
using System.Linq;

namespace DSA.Model.Dto
{
    public class BrowserData
    {
        public BrowserData()
        {
            Children = Enumerable.Empty<BrowserData>();
        }
        public string ID { get; set; }
        public int Order { get; set; }
        public string Name { get; set; }
        public BrowserDataType Type { get; set; }
        public IEnumerable<BrowserData> Children { get; set; }
        public MediaLink Media { get; set; }
    }

    public enum BrowserDataType
    {
        Category,
        Media,
    }
}
