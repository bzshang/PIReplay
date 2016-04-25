using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System.Timers;
using OSIsoft.AF;
using System.Collections.Concurrent;
using System.Threading;
using System.Configuration;

namespace PIReplayLib
{
    /// <summary>
    /// Coordinates the PIReader and PIWriter. 
    /// </summary>
    public class PIReplayer
    {
        private PIServer _sourceServer;
        private PIPointList _sourcePoints;

        private PIServer _destServer;
        private PIPointList _destPoints;

        private PIReader _reader;
        private PIWriter _writer;

        /// <summary>
        /// Connect to source and destination PI Data Archives. Find the source and destination PI Points.
        /// Instantiate the PIReader and PIWriter.
        /// </summary>
        public PIReplayer()
        {
            try
            {
                _sourceServer = new PIServers()[ConfigurationManager.AppSettings["sourceServer"]];
                _sourceServer.Connect();

                _destServer = new PIServers()[ConfigurationManager.AppSettings["destServer"]];
                _destServer.Connect();
            }
            catch (PIConnectionException ex)
            {
                Logger.Write(ex.ToString());
            }

            Logger.Write("Loading points");

            _sourcePoints = new PIPointList(PIPoint.FindPIPoints(
                piServer: _sourceServer,
                nameFilter: ConfigurationManager.AppSettings["sourceNameFilter"],
                sourceFilter: ConfigurationManager.AppSettings["sourcePS"])
                );
            _destPoints = new PIPointList(PIPoint.FindPIPoints(
                piServer: _destServer,
                nameFilter: ConfigurationManager.AppSettings["destNameFilter"],
                sourceFilter: ConfigurationManager.AppSettings["destPS"])
                );

            Logger.Write(string.Format("Done loading {0} points", _sourcePoints.Count));

            // The PIReader passes the data to the PIWriter via the DataQueue.
            DataQueue queue = new DataQueue();
            _reader = new PIReader(this, _sourceServer, _sourcePoints, _destServer, _destPoints, queue);
            _writer = new PIWriter(this, _sourceServer, _sourcePoints, _destServer, _destPoints, queue);
        }

        public void Start()
        {
            _writer.Start();

            Logger.Write("Started reader and writer");
        }

        public void RequestFill(bool initial = false)
        {
            _reader.GetPages(initial);
        }
    }
}
