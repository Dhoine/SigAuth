using System.Collections.Generic;
using SharedClasses;

namespace FeatureExtractor
{
    public interface IFeatureExtractor
    {
        DtwFeatures GetDTWFeatures(RawPoint[][] sample);
    }
}