// //////////////////////////////////////////////////////////////////
// Provide message for updating configuration synching in detailed view
// //////////////////////////////////////////////////////////////////

namespace DSA.Model.Messages
{
    public class SynchronizationDetailMessage
    {
        public SynchronizationDetailMessage(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
