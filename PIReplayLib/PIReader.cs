using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Collections.Concurrent;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System.Reflection;

namespace PIReplayLib
{
    public class PIReader
    {
        private PIReplayer _replayer;

        //private Timer _timer;
        private double _period = 120;

        private PIServer _sourceServer;
        private PIPointList _sourcePoints;

        private PIServer _destServer;
        private PIPointList _destPoints;

        private DataQueue _queue;

        private int _lookAheadMinutes = 5;
        private int _interval = 10;

        public PIReader(PIReplayer replayer,
            PIServer sserver, PIPointList spoints,
            PIServer dserver, PIPointList dpoints,
            DataQueue queue)
        {
            _replayer = replayer;

            _sourceServer = sserver;
            _sourcePoints = spoints;

            _destServer = dserver;
            _destPoints = dpoints;

            //_timer = new Timer();
            //_timer.Elapsed += GetPages;
            //_timer.Interval = Utils.FindInterval(_period);

            _queue = queue;
        }

        //public void Start()
        //{
        //    _timer.Start();
        //}

        public void GetPages(bool initial = false)
        {
            Logger.Write(string.Format("Entering {0}.{1}", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name));
            GetPages(_queue.LatestTime.LocalTime, initial);
        }

        //private void GetPages(object sender, ElapsedEventArgs e)
        //{
        //    _timer.Stop();

        //    Logger.Write(string.Format("Timer called GetPages at {0}", e.SignalTime));

        //    DateTime signalTime = e.SignalTime;
        //    DateTime currTime = signalTime.Truncate(TimeSpan.FromSeconds(1));
        //    DateTime requestStartTime = currTime.AddYears(-1);

        //    GetPages(requestStartTime);

        //    _timer.Interval = Utils.FindInterval(_period);
        //    _timer.Start();
        //}

        private void GetPages(DateTime startTime, bool initial = false)
        {
            int addMinutes = _lookAheadMinutes;
            if (initial) addMinutes = _lookAheadMinutes*2;

            DateTime historicalStartTime = startTime.AddYears(-1);
            DateTime historicalEndTime = historicalStartTime.AddMinutes(addMinutes);

            Logger.Write(string.Format("Getting page for {0} - {1}", 
                historicalStartTime.AddYears(1), historicalEndTime.AddYears(1)));

            AFTimeRange timeRange = new AFTimeRange(new AFTime(historicalStartTime), new AFTime(historicalEndTime));
            AFTimeSpan timeSpan = new AFTimeSpan(TimeSpan.FromSeconds(_interval), new AFTimeZone());

            // Transpose the returned IEnumerable<AFValues> (each list item has same PI Point) into 
            // IList<DataRecord> (each list item has same timestamp)
            IList<DataRecord> records = _sourcePoints
                .InterpolatedValues(
                    timeRange: timeRange,
                    interval: timeSpan,
                    filterExpression: String.Empty,
                    includeFilteredValues: true,
                    pagingConfig: new PIPagingConfiguration(PIPageType.TagCount, 1000))
                .Select(vals =>
                {
                    foreach (var v in vals)
                    {
                        v.Timestamp = v.Timestamp.LocalTime.AddYears(1); ;
                    }
                    return vals;
                })
                .SelectMany(vals => vals)
                .GroupBy(v => v.Timestamp)
                .Select(grp => DataRecord.Create(grp.Key, grp.ToList()))
                .OrderBy(rec => rec.Time)
                .ToList();

            _queue.Add(records);
            Logger.Write(string.Format("Added {0} records", records.Count));
        }
    }
}
