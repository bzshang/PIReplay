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
using PIReplayLib;

namespace PIReplayConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.ConfigureLogging();

            PIReplayer replayer = new PIReplayer();
            replayer.Start();

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();

            Logger.Close();
        }
    }
}
