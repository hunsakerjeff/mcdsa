namespace DSA.Model.Messages
{
    public class SwipeMessage
    {
        public static LeftSwipe Left
        {
            get { return new LeftSwipe(); }
        }

        public static RightSwipe Right
        {
            get { return new RightSwipe(); }
        }

        public class LeftSwipe : SwipeMessage
        {
          
        }

        public class RightSwipe : SwipeMessage
        {
           
        }
    }
}
