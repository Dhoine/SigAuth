using System;
using System.Collections.Generic;
using System.Linq;
using EpwLib;
using SharedClasses;

namespace FeatureExtractor
{
    public class Extractor : IFeatureExtractor
    {
        

        public EpwFeature GetEpwFeatures(List<ExtremePoint> sample)
        {
            var epwFeature = new EpwFeature();
            var epw = new Epw();
            epwFeature.ExtremePoints = epw.FilterExtremePoints(epw.GetExtremePointsUnfiltered(epw.SmoothPoints(sample)));
            return epwFeature;
        }

        #region Helpers

        #endregion
    }
}