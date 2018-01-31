using DSA.Model.Enums;

namespace DSA.Model.Dto
{
    public class MediaWrapper : IHaveMedia
    {
        public MediaLink Media
        {
            get;
            set;
        }
    }
}
