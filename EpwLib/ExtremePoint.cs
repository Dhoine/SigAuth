using System;
using System.Collections.Generic;
using System.Text;
using SharedClasses;

namespace EpwLib
{
    public enum ExtremePointType
    {
        StartPoint,
        EndPoint,
        VerticalMin,
        VerticalMax,
        HorizontalMin,
        HorizontalMax,
        VerticalPlateauStartMin,
        VerticalPlateauStartMax,
        VerticalPlateauEndMin,
        VerticalPlateauEndMax,
        HorizontalPlateauStartMin,
        HorizontalPlateauStartMax,
        HorizontalPlateauEndMin,
        HorizontalPlateauEndMax,
        Inflection,
        Curvature
    }
    public class ExtremePoint
    {
        public RawPoint Point { get; set; }
        public ExtremePointType Type { get; set; }
        public PointDynamicFeatures Features { get; set; }
    }
}
