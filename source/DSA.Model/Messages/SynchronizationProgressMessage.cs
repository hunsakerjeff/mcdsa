namespace DSA.Model.Messages
{
    public class SynchronizationProgressMessage
    {
        public SynchronizationProgressMessage(decimal current, decimal total)
        {
            Current = current;
            Total = total;
        }

        public decimal Current { get; private set; }
        public decimal Total { get; private set; }
    }
}
