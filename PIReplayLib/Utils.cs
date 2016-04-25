using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;

namespace PIReplayLib
{
    public static class Utils
    {
        /// <summary>
        /// Get the milliseconds to the next trigger time. 
        /// For example, if period = 5 seconds, we want the trigger times to be at 0, 5, 10, etc. seconds past the minute. 
        /// Ex. If the current time is at 12 seconds past the minute, then return 3000 milliseconds.
        /// Therefore, the timer will trigger at 15 seconds past the minute.
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        public static double FindInterval(double period)
        {
            double totalMilliseconds;

            DateTime currentTime = DateTime.Now;
            double seconds = currentTime.Second;
            double secondsToAdd = period - (seconds % period);
            double milliseconds = currentTime.Millisecond;

            DateTime projected = currentTime.AddSeconds(secondsToAdd).AddMilliseconds(-milliseconds);

            totalMilliseconds = (projected - currentTime).TotalMilliseconds;

            return totalMilliseconds;
        }

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> seq, Action<string> Logger, string message)
        {
            Logger(message);
            return seq;
        }
    }
}
