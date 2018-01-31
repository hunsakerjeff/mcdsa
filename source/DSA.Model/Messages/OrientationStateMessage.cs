using DSA.Model.Enums;

namespace DSA.Model.Messages
{
    public class OrientationStateMessage
    {
        public PageOrientations Orientation
        {
            get;
            private set;
        }

        public OrientationStateMessage(PageOrientations orientation)
        {
            Orientation = orientation;
        }
    }
}