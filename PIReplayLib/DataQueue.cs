
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using OSIsoft.AF.Time;
using System.Reflection;

namespace PIReplayLib
{
    public class DataQueue
    {
        private ConcurrentQueue<DataRecord> _queue;

        public int Count { get { return _queue.Count; } }

        // Most recent timestamp of value in the queue.
        // This allows PIReader to know the start time of the next query.
        public AFTime LatestTime { get; private set; }

        public DataQueue()
        {
            _queue = new ConcurrentQueue<DataRecord>();

            LatestTime = new AFTime(DateTime.Now.Truncate(TimeSpan.FromSeconds(1)));
        }

        public void Add(IList<DataRecord> records)
        {
            Logger.Write(string.Format("Entering {0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name));
            foreach (var rec in records)
            {
                _queue.Enqueue(rec);
                if (LatestTime.CompareTo(rec.Time) < 0)
                {
                    LatestTime = rec.Time;
                }

            }
            Logger.Write(string.Format("Queue synced to {0}", LatestTime));
        }

        public IList<DataRecord> RemoveAtAndBefore(AFTime syncTime)
        {
            Logger.Write(string.Format("Entering {0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name));
            List<DataRecord> returnedRecs = new List<DataRecord>();

            AFTime pointerRecTime = AFTime.MinValue;
            
            while (pointerRecTime.CompareTo(syncTime) <= 0 || _queue.Count == 0)
            {
                DataRecord rec = null;
                bool peekResult = _queue.TryPeek(out rec);

                if (rec != null)
                {
                    pointerRecTime = rec.Time;
                    if (rec.Time.CompareTo(syncTime) <= 0)
                    {                    
                        rec = null;
                        _queue.TryDequeue(out rec);
                        if (rec != null)
                        {
                            returnedRecs.Add(rec);
                        }
                    }    
                }
                else
                {
                    break;
                }
            }

            return returnedRecs;
        }

    }
}
