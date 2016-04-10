using System;
using System.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    internal class FixedSizedQueue<T>
    {
        ConcurrentQueue<T> q = new ConcurrentQueue<T>();

        T lastEnqueued = default(T);

        public int Limit { get; set;  }

        public T GetLastEnqueued()
        {
            return lastEnqueued;
        }

        public bool Contains(T obj)
        {
            foreach (T o in q)
            {
                if (o.Equals(obj))
                {
                    return true;
                }
            }
            return false;   
        }

        public void Clear()
        {
            q = new ConcurrentQueue<T>();
        }

        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            lock (this)
            {
                T overflow;
                while (q.Count > Limit && q.TryDequeue(out overflow));
            }
            lastEnqueued = obj;
        }
    }
}