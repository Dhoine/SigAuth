using System.Collections.Generic;
using EpwLib;

namespace SharedClasses
{
    public class EpwModel
    {
        public List<EpwFeature> Samples { get; set; }
        public List<NameMinMax> MinMaxFeatures { get; set; }
        public bool IsDirty { get; set; }
    }

    public class EpwFeature
    {
        public List<ExtremePoint> ExtremePoints { get; set; }
    }
}