using System.Collections.Generic;
using DSA.Model.Models;

namespace DSA.Model.Dto
{
    public class CategoryInfo
    {
        public CategoryMobileConfig Config { get; set; }
        public Category Category { get; set; }
        public List<DocumentInfo> Documents { get; set; }
    }
}
