using System.Collections.Generic;

namespace DSA.Model.Dto
{
    public class CategoryBrowserDTO : CategoryDTO
    {
        public IEnumerable<CategoryBrowserDTO> SubCategories { get; set; }
    }
}
