using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;

namespace PIReplayLib
{
    /// <summary>
    /// Encapsulates all interpolated AFValue(s) of source points of interest at a specific timestamp.
    /// </summary>
    public class DataRecord
    {
        public AFTime Time { get; set; }
        public IList<AFValue> Values { get; set; }

        public static DataRecord Create(AFTime time, IList<AFValue> values)
        {
            return new DataRecord { Time = time, Values = values };
        }
    }
}
