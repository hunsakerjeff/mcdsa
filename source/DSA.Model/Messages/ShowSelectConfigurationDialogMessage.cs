using DSA.Model.Enums;

namespace DSA.Model.Messages
{
    public class SynchronizationFinished
    {
        public SynchronizationMode Mode { get; set; }
        public bool AutoSync { get; set; }
    }
}
