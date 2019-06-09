using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning;
using SharedClasses;

namespace SparseDtwLib
{
    public class SparseDtw : ISignatureVerification
    {
        public List<string> CompareFeatureList = new List<string>
            {GlobalConstants.Sin, /*Cos, QDir,*/ GlobalConstants.Speed};

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature,
            List<List<RawPoint>> checkedSample)
        {

            if (CompareFeatureList == null || !CompareFeatureList.Any())
                return false;
            var features = new List<DtwFeatures>();

            foreach (var sample in origSignature) features.Add(GetDTWFeatures(sample.Sample));

            foreach (var feature in CompareFeatureList)
            {
                var outputs = new int[origSignature.Count];
                var knn = new KNearestNeighbors(origSignature.Count, new DwtComparer()) {NumberOfClasses = 1};
                var inputs = features.Select(f => f.Features.Select(d => d.FeaturesDict[feature]).ToArray()).ToArray();
                knn.Learn(inputs, outputs);

                var test = GetDTWFeatures(checkedSample);
                var tst = knn.Scores(test.Features.Select(f => f.FeaturesDict[feature]).ToArray());
                if (tst.First() < 1.95)
                    return false;
            }

            return true;
        }

        private DtwFeatures GetDTWFeatures(List<List<RawPoint>> sample)
        {
            var features = new DtwFeatures
            {
                Features = new List<PointDynamicFeatures>()
            };
            foreach (var stroke in sample)
                for (var i = 1; i < stroke.Count; i++)
                    features.Features.Add(new PointDynamicFeatures
                    {
                        [GlobalConstants.Sin] = FeatureFunctions.Sin(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Cos] = FeatureFunctions.Cos(stroke[i], stroke[i - 1]),
                        [GlobalConstants.QDir] = FeatureFunctions.QDir(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Speed] = FeatureFunctions.Speed(stroke[i], stroke[i - 1])
                    });
            return features;
        }
    }
}