using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Util
{
    /// <summary>
    /// Prioritized Queue
    /// Not ThreadSafe!
    /// </summary>
    /// <typeparam name="T">Queue object type</typeparam>
    public class PriorityQueue<T> 
    {
        private Dictionary<Priority, Queue<T>> PrioritizedQueues = new Dictionary<Priority, Queue<T>>();

        public int Count
        {
            get
            {
                return (from regPriority in PrioritizedQueues.Keys
                        select PrioritizedQueues[regPriority].Count).Sum();
            }
        }

        public void Enqueue(T obj, Priority p)
        {
            if (!PrioritizedQueues.ContainsKey(p))
            {
                PrioritizedQueues.Add(p, new Queue<T>());
            }
            PrioritizedQueues[p].Enqueue(obj);
        }

        /// <summary>
        /// Get the highest priority queue with items in it
        /// </summary>
        private Queue<T> HighestOrderQueue
        {
            get
            {
                return (from p in PrioritizedQueues.Keys
                 where PrioritizedQueues[p].Count > 0
                 orderby (int)p descending
                 select PrioritizedQueues[p]).FirstOrDefault();                 
            }
        }

        public T Dequeue()
        {
            var queue = HighestOrderQueue;
            if (queue != null && queue.Count > 0)
            {
                return queue.Dequeue();
            }
            return default(T);
        }

        public void Clear()
        {
            foreach (Priority p in PrioritizedQueues.Keys)
                PrioritizedQueues[p].Clear();
        }
    }



    /// <summary>
    /// Specifies the queue that the packet will be placed in
    /// </summary>
    public enum Priority : byte
    {
        Low,
        Normal,
        High,
        Realtime
    }
}
