using System.Collections.Generic;

namespace SparseDtwLib
{
    public class DtwThreadParams
    {
        public List<double> FirstSequence { get; set; }
        public List<double> SecondSequence { get; set; }
        public double Res { get; set; }
    }
}