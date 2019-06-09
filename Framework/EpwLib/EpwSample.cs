using System;
using System.Collections.Generic;
using Accord.IO;

namespace EpwLib
{
    public class EpwSample : ICloneable
    {
        public List<ExtremePoint> Points { get; set; }

        public object Clone()
        {
            return Points.DeepClone();
        }
    }
}