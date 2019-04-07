using System.Collections.Generic;
using SharedClasses;

namespace FeatureExtractor
{
    public interface IFeatureExtractor
    {
        DtwFeatures GetDTWFeatures(List<List<RawPoint>> sample);
    }
}