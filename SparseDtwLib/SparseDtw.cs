using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SharedClasses;

namespace SparseDtwLib
{
    public class SparseDtw
    {
        private List<string> _compareFeatureList = new List<string> { GlobalConstants.Sin, /*Cos, QDir,*/ GlobalConstants.Speed };
        private readonly List<string> _fullFeatureList = new List<string> { GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed };

        private async Task<NameMinMax> GetFeatureMinMaxAvg(string featureName,
            IReadOnlyList<DtwFeatures> features)
        {
            var avgMin = 0d;
            var avgMax = 0d;
            
            for (var i = 0; i < features.Count; i++)
            {
                var distances = new List<double>();
                var tasks = new List<Task<double>>();

                for (var j = 0; j < features.Count; j++)
                {
                    if (i == j) continue;
                    var closureI = i;
                    var closureJ = j;
                    tasks.Add(Task.Run(async () => await CompareSequences(features[closureI].Features.Select(f => f[featureName]), features[closureJ].Features.Select(f => f[featureName]), 0.5)));
                }
                Task.WaitAll(tasks: tasks.ToArray());
                foreach (var task in tasks)
                {
                    distances.Add(task.Result);
                }
                avgMin += distances.Min();
                avgMax += distances.Max();
            }

            avgMin /= features.Count;
            avgMax /= features.Count;
            return new NameMinMax{Name = featureName, Min = avgMin, Max = avgMax};
        }

        public List<NameMinMax> BuildModel(List<SignatureSampleDeserialized> origSignature)
        {
            var features = new List<DtwFeatures>();
            
            var model = new List<NameMinMax>();
            foreach (var sample in origSignature)
            {
                features.Add(GetDTWFeatures(sample.Sample));
            }
            var tasks = new List<Task<NameMinMax>>();
            foreach (var featureName in _fullFeatureList)
            {
                tasks.Add(Task.Run(async () => await GetFeatureMinMaxAvg(featureName, features)));
            }

            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                model.Add(task.Result);
            }

            return model;
        }

        private List<NameMinMax> GetCheckedModel(List<SignatureSampleDeserialized> origSignature,
            List<List<RawPoint>> checkedSample)
        {
            var features = new List<DtwFeatures>();
            var checkedFeatures = GetDTWFeatures(checkedSample);

            var model = new List<NameMinMax>();
            foreach (var sample in origSignature)
            {
                features.Add(GetDTWFeatures(sample.Sample));
            }
            
            foreach (var featureName in _compareFeatureList)
            {
                var distances = new List<double>();
                var tasks = new List<Task<double>>();
                foreach (var feature in features)
                {
                    tasks.Add(Task.Run(async () => await CompareSequences(feature.Features.Select(f => f[featureName]), checkedFeatures.Features.Select(f => f[featureName]), 0.5)));
                }

                Task.WaitAll(tasks: tasks.ToArray());
                foreach (var task in tasks)
                {
                    distances.Add(task.Result);
                }
                var sMin = distances.Min();
                var sMax = distances.Max();
                model.Add(new NameMinMax { Name = featureName, Min = sMin, Max = sMax });
            }

            return model;
        } 

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample, List<string> featuresToCompare,
            List<NameMinMax> featuresMinMax = null)
        {
            if (featuresToCompare != null && featuresToCompare.Any())
            {
                _compareFeatureList = featuresToCompare;
            }
            if (featuresMinMax == null || !featuresMinMax.Any())
            {
                featuresMinMax = BuildModel(origSignature);
            }

            var model = GetCheckedModel(origSignature, checkedSample);

            var diffValues = FeatureFunctions.GetDiffValues(featuresMinMax, model);
            var total = 0d;
            foreach (var diff in diffValues)
            {
                total += diff.Min + diff.Max;
            }
            return total < 0.06;
        }

        public async Task<double> CompareSequences(IEnumerable<double> sEnumerable, IEnumerable<double> qEnumerable, double res)
        {
            var s = sEnumerable.ToList();
            var q = qEnumerable.ToList();
            var SMMatrix = Enumerable.Repeat(-1d, s.Count * q.Count).ToArray();
            Array.Clear(SMMatrix,0, s.Count * q.Count);
            var sQ = Helper.Quantize(s);
            var qQ = Helper.Quantize(q);
            var lowerBound = 0.0;
            var upperBound = res;
            while (lowerBound >= 0 && lowerBound <= 1 - res/2)
            {
                var sIndxs = Helper.FindIndexes(sQ, lowerBound, upperBound);
                var qIndxs = Helper.FindIndexes(qQ, lowerBound, upperBound);
                lowerBound = lowerBound + res / 2;
                upperBound = lowerBound + res;
                foreach (var sIndex in sIndxs)
                {
                    foreach (var qIndex in qIndxs)
                    {
                        var euc = FeatureFunctions.EucDist(s[sIndex], q[qIndex]);
                        SMMatrix[s.Count * qIndex + sIndex] = euc;
                    }
                }
            }

            for (var i = 0; i < SMMatrix.Length; i++)
            {
                if (SMMatrix[i] < 0) continue;
                var lowerNeighbors = Helper.TryGetLowerNeighbors(SMMatrix, i, s.Count);
                if (lowerNeighbors.Any())
                {
                    var min = lowerNeighbors.Min(n => n.Value);
                    SMMatrix[i] += min;
                }
                
                var upperNeighbors = Helper.TryGetUpperNeighbors(SMMatrix, i, s.Count);
                if (upperNeighbors.Any(n => n.Value >= 0)) continue;
                foreach (var neighbor in upperNeighbors)
                {
                    SMMatrix[neighbor.Index] =
                        FeatureFunctions.EucDist(s[neighbor.Index % s.Count], q[neighbor.Index / s.Count]);
                }
            }

            return SMMatrix[s.Count * q.Count - 1];
        }

        private DtwFeatures GetDTWFeatures(List<List<RawPoint>> sample)
        {
            var features = new DtwFeatures
            {
                Features = new List<PointDynamicFeatures>()
            };
            foreach (var stroke in sample)
            {
                for (var i = 1; i < stroke.Count; i++)
                {
                    features.Features.Add(new PointDynamicFeatures
                    {
                        [GlobalConstants.Sin] = FeatureFunctions.Sin(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Cos] = FeatureFunctions.Cos(stroke[i], stroke[i - 1]),
                        [GlobalConstants.QDir] = FeatureFunctions.QDir(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Speed] = FeatureFunctions.Speed(stroke[i], stroke[i - 1])
                    });
                }
            }
            return features;
        }
    }
}
