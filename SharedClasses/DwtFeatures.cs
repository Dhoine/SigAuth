using System.Collections.Generic;
using MatrixLib;

namespace SharedClasses
{
    public class DwtFeatures
    {
        public List<Matrix<List<double>>> SamplesCoefficients { get; set; }
        public bool IsDirty { get; set; }
    }
}