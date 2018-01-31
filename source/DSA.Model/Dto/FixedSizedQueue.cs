using System.Collections.Concurrent;

namespace DSA.Model.Dto
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object _syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (_syncObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }

        public void Clear()
        {
            T outObj;
            for (int i = 0; i < Size; i++)
            {
                base.TryDequeue(out outObj);
            }
        }
    }
}
