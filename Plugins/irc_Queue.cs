using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace VhaBot.Plugins
{
    public class IRCQueue : PluginBase
    {
        public IRCQueue()
        {

        }

        [Serializable]
        public class PrioQueue<T> : ICollection, IEnumerable
        {
            public PrioQueue()
            {
                this.internalListLow = new List<T>();
                this.internalListNormal = new List<T>();
                this.internalListHigh = new List<T>();
            }

            public PrioQueue(ICollection<T> coll)
            {
                internalListLow = new List<T>(coll);
                internalListNormal = new List<T>(coll);
                internalListHigh = new List<T>(coll);
            }


            protected List<T> internalListLow = null;
            protected List<T> internalListNormal = null;
            protected List<T> internalListHigh = null;


            public int CountPrio(Priority prio)
            {

                switch (prio)
                {
                    case Priority.Low:
                        return internalListLow.Count;
                    case Priority.Normal:
                        return internalListNormal.Count;
                    case Priority.High:
                        return internalListHigh.Count;
                    default:
                        return 0;
                }
            }

            public virtual int Count
            {
                get { return (internalListLow.Count + internalListNormal.Count + internalListHigh.Count); }
            }

            public virtual bool IsSynchronized
            {
                get { return (false); }
            }

            public virtual object SyncRoot
            {
                get { return (this); }
            }

            public virtual void Clear()
            {
                internalListLow.Clear();
                internalListNormal.Clear();
                internalListHigh.Clear();
            }

            public virtual bool Contains(T obj)
            {
                throw (new NotSupportedException("Contains is not supported in Prio Queue"));
            }

            public virtual void CopyTo(Array array, int index)
            {
                throw (new NotSupportedException("CopyTo is not supported in Prio Queue"));
            }

            public virtual T Dequeue()
            {
                if (internalListHigh.Count > 0)
                    return Dequeue(Priority.High);
                else if (internalListNormal.Count > 0)
                    return Dequeue(Priority.Normal);
                else
                    return Dequeue(Priority.Low);
            }

            public virtual T Dequeue(Priority prio)
            {
                T retObj;
                switch (prio)
                {
                    case Priority.Low:
                        retObj = internalListLow[0];
                        internalListLow.RemoveAt(0);
                        return (retObj);
                    case Priority.Normal:
                        retObj = internalListNormal[0];
                        internalListNormal.RemoveAt(0);
                        return (retObj);
                    case Priority.High:
                        retObj = internalListHigh[0];
                        internalListHigh.RemoveAt(0);
                        return (retObj);
                    default:
                        return default(T);
                }
            }

            public virtual void Enqueue(Priority prio, T obj)
            {
                switch (prio)
                {
                    case Priority.Low:
                        internalListLow.Add(obj);
                        break;
                    case Priority.Normal:
                        internalListNormal.Add(obj);
                        break;
                    case Priority.High:
                        internalListHigh.Add(obj);
                        break;
                }
            }

            public virtual IEnumerator GetEnumerator()
            {
                throw (new NotSupportedException("GetEnumerator is not supported in Prio Queue"));
            }

            public virtual T[] ToArray()
            {
                throw (new NotSupportedException("ToArray is not supported in Prio Queue"));
            }

            public virtual void TrimExcess()
            {
                internalListLow.TrimExcess();
                internalListNormal.TrimExcess();
                internalListHigh.TrimExcess();
            }
        }

        public class IRCQueueItem
        {
            public string Target;
            public string Message;
            public IRCQueueItem(string Target, string Message)
            {
                this.Target = Target;
                this.Message = Message;
            }
        }

        public enum Priority
        {
            High = 5, Normal = 3, Low = 1
        }
    }
}