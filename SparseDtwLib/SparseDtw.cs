using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FeatureExtractor;
using SharedClasses;

namespace SparseDtwLib
{
    public class SparseDtw
    {
        private const string Sin = "sin";
        private const string Cos = "cos";
        private const string QDir = "qdir";
        private const string Speed = "speed";

        private readonly List<string> _featureList = new List<string> { Sin, /*Cos, QDir,*/ Speed };

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
                    switch (featureName)
                    {
                        case Sin:
                            
                            tasks.Add(Task.Run(async () => await CompareSequences(features[closureI].Features.Select(f => f.Sin), features[closureJ].Features.Select(f => f.Sin), 0.5)));
                            break;
                        case Cos:
                            tasks.Add(Task.Run(async () => await CompareSequences(features[closureI].Features.Select(f => f.Cos), features[closureJ].Features.Select(f => f.Cos), 0.5)));
                            break;
                        case QDir:
                            tasks.Add(Task.Run(async () => await CompareSequences(features[closureI].Features.Select(f => f.QDirs), features[closureJ].Features.Select(f => f.QDirs), 0.5)));
                            break;
                        case Speed:
                            tasks.Add(Task.Run(async () => await CompareSequences(features[closureI].Features.Select(f => f.Speeds), features[closureJ].Features.Select(f => f.Speeds), 0.5)));
                            break;
                        default:
                            break;
                    }
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
            var extractor = new Extractor();
            
            var model = new List<NameMinMax>();
            foreach (var sample in origSignature)
            {
                features.Add(extractor.GetDTWFeatures(sample.Sample));
            }
            var tasks = new List<Task<NameMinMax>>();
            foreach (var featureName in _featureList)
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
            var extractor = new Extractor();
            var checkedFeatures = extractor.GetDTWFeatures(checkedSample);

            var model = new List<NameMinMax>();
            foreach (var sample in origSignature)
            {
                features.Add(extractor.GetDTWFeatures(sample.Sample));
            }
            
            foreach (var featureName in _featureList)
            {
                var distances = new List<double>();
                var tasks = new List<Task<double>>();
                foreach (var feature in features)
                {
                    switch (featureName)
                    {
                        case Sin:
                            tasks.Add(Task.Run(async () => await CompareSequences(feature.Features.Select(f => f.Sin), checkedFeatures.Features.Select(f => f.Sin), 0.5)));
                            break;
                        case Cos:
                            tasks.Add(Task.Run(async () => await CompareSequences(feature.Features.Select(f => f.Cos), checkedFeatures.Features.Select(f => f.Cos), 0.5)));
                            break;
                        case QDir:
                            tasks.Add(Task.Run(async () => await CompareSequences(feature.Features.Select(f => f.QDirs), checkedFeatures.Features.Select(f => f.QDirs), 0.5)));
                            break;
                        case Speed:
                            tasks.Add(Task.Run(async () => await CompareSequences(feature.Features.Select(f => f.Speeds), checkedFeatures.Features.Select(f => f.Speeds), 0.5)));
                            break;
                        default:
                            break;
                    }
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

        private List<NameMinMax> GetDiffValues(List<NameMinMax> featuresMinMax, List<NameMinMax> checkedFeaturesMinMax)
        {
            var res = new List<NameMinMax>();
            foreach (var feature in checkedFeaturesMinMax)
            {
                var correspondingFeature = featuresMinMax.First(f => f.Name.Equals(feature.Name));
                var diffMin = (feature.Min - correspondingFeature.Min) / correspondingFeature.Min;
                var diffMax = (feature.Max - correspondingFeature.Max) / correspondingFeature.Max;
                res.Add(new NameMinMax{Name = feature.Name, Min = diffMin, Max = diffMax});
            }

            return res;
        }

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample,
            List<NameMinMax> featuresMinMax = null)
        {
            if (featuresMinMax == null || !featuresMinMax.Any())
            {
                featuresMinMax = BuildModel(origSignature);
            }

            var model = GetCheckedModel(origSignature, checkedSample);

            var diffValues = GetDiffValues(featuresMinMax, model);
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
                        var euc = Helper.EucDist(s[sIndex], q[qIndex]);
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
                        Helper.EucDist(s[neighbor.Index % s.Count], q[neighbor.Index / s.Count]);
                }
            }

            return SMMatrix[s.Count * q.Count - 1];
        }
    }
}
