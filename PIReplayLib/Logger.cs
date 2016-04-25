using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PIReplayLib
{
    public static class Logger
    {
        public static TraceSource TraceSrc;
        public static EventLogTraceListener EventLogListener;

        public static void ConfigureLogging()
        {
            if (!EventLog.SourceExists("PIReplay"))
            {
                EventLog.CreateEventSource("PIReplay", "PIReplay Service");
            }
            
            TraceSrc = new TraceSource("pireplayServiceSource");
            //System.IO.File.WriteAllText("log.txt", string.Format("{0}", TraceSrc == null));
            EventLogListener = (EventLogTraceListener)TraceSrc.Listeners["eventLogListener"];
            //System.IO.File.WriteAllText("log.txt", TraceSrc.Listeners[0].Name);
            EventLogListener.EventLog.Log = "PIReplay Service";
        }

        public static void Write(string message)
        {
            Write(TraceEventType.Information, 0, message);
        }

        public static void Write(TraceEventType eventType, int id, string message)
        {
            TraceSrc.TraceEvent(eventType, id, message);
        }

        public static void Close()
        {
            TraceSrc.Close();
        }

    }
}
