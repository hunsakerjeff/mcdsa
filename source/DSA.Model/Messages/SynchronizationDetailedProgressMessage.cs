// //////////////////////////////////////////////////////////////////
// Provide message for updating content synching in detailed view
// //////////////////////////////////////////////////////////////////

namespace DSA.Model.Messages
{
    public class SynchronizationDetailedProgressMessage
    {
        public SynchronizationDetailedProgressMessage(string itemName, decimal size)
        {
            ItemName = itemName;
            Size = size;
        }

        public string ItemName { get; private set; }
        public decimal Size { get; private set; }
    }
}
