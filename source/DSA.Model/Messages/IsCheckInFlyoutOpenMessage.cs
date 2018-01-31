namespace DSA.Model.Messages
{
    public class IsCheckInFlyoutOpenMessage
    {
        public IsCheckInFlyoutOpenMessage(bool isOpen)
        {
            IsOpen = isOpen;
        }

        public bool IsOpen {get; private set;}
    }
}
