using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Timers;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Data;
using OSIsoft.AF.Time;

namespace PIReplayLib
{
    public class PIWriter
    {
        private PIReplayer _replayer;

        private System.Timers.Timer _timer;
        private double _period = 5; // in seconds

        private PIServer _sourceServer;
        private PIPointList _sourcePoints;

        private PIServer _destServer;
        private PIPointList _destPoints;

        private DataQueue _queue;

        private Task _requestFill;

        // Source and destination PI Points are matched simply by point name.
        private Dictionary<PIPoint, PIPoint> _sourceToDest;

        public PIWriter(PIReplayer replayer, 
            PIServer sserver, PIPointList spoints,
            PIServer dserver, PIPointList dpoints,
            DataQueue queue)
        {
            _replayer = replayer;

            _sourceServer = sserver;
            _sourcePoints = spoints;

            _destServer = dserver;
            _destPoints = dpoints;

            _timer = new System.Timers.Timer();
            _timer.Elapsed += WriteValues;
            _timer.Interval = Utils.FindInterval(_period);

            _queue = queue;

        }

        public void Start()
        {
            _timer.Start();

            // Request via the PIReplayer to fill the DataQueue. 
            // The PIReplayer will call the PIReader to read from source server and fill the queue.
            _requestFill = Task.Run(() => _replayer.RequestFill(initial: true));

            _sourceToDest = new Dictionary<PIPoint, PIPoint>();

            Dictionary<string, PIPoint> destPointNameLookup = _destPoints
                .GroupBy(p => p.Name)
                .ToDictionary(grp => grp.Key, grp => grp.First());
 
            foreach (PIPoint sourcePt in _sourcePoints)
            {
                _sourceToDest[sourcePt] = destPointNameLookup[sourcePt.Name];
            }
        }

        /// <summary>
        /// This is called every 5 seconds via the timer callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteValues(object sender, ElapsedEventArgs e)
        {
            Logger.Write(string.Format("Current queue count: {0}", _queue.Count));
            _timer.Stop();       

            // If data queue running low, request another fill.
            if (_queue.Count < 60)
            {
                if (_requestFill.IsCompleted || _requestFill.IsFaulted || _requestFill.IsCanceled)
                {
                    if (_requestFill.IsCanceled || _requestFill.IsFaulted)
                    {
                        Logger.Write("Cancelled or faulted");
                    }
                    _requestFill = Task.Run(() => _replayer.RequestFill());
                }          
            }

            AFTime syncTime = new AFTime(e.SignalTime);

            // Remove all records at and before the timer trigger (signal) time.
            IList<DataRecord> records = _queue.RemoveAtAndBefore(syncTime);

            Logger.Write(string.Format("Removed {0} records", records.Count));

            if (records.Count == 0)
            {
                _timer.Interval = Utils.FindInterval(_period);
                Logger.Write(string.Format("Next call in {0}", _timer.Interval));
                _timer.Start();
                return;
            }

            // Flatten the DataRecord in a list of AFValue(s)
            List<AFValue> valsList = records.SelectMany(rec => rec.Values).ToList();

            // Set the PIPoint property of the AFValue to the destination server PI Point
            foreach (var v in valsList)
            {
                v.PIPoint = _sourceToDest[v.PIPoint];
            }

            // Divide the AFValue list into 5 chunks. 
            // Wait 500 milliseconds before writing the next chunk to avoid sending too much data over the network at once.
            int chunkSize  = valsList.Count / 5;
            List<List<AFValue>> valsChunks = valsList.ChunkBy(chunkSize);

            int updated = 0;
            foreach (var chunk in valsChunks)
            {
                AFErrors<AFValue> errors = _destServer.UpdateValues(chunk, AFUpdateOption.InsertNoCompression, AFBufferOption.Buffer);
                
                if (errors != null && errors.HasErrors)
                {
                    foreach (var kvp in errors.Errors.Take(1))
                    {
                        Logger.Write(string.Format("Attr: {0}, Ex: {1}",
                            kvp.Key.Attribute.GetPath(), kvp.Value.Message));
                    }
                }
                else
                {
                    updated += chunk.Count;
                }
                Thread.Sleep(500);
            }
            Logger.Write(string.Format("Updated {0} tags", updated));

            _timer.Interval = Utils.FindInterval(_period);
            Logger.Write(string.Format("Next call in {0}", _timer.Interval));
            _timer.Start();
        }
    }

    public static class ListExtensions
    {
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
