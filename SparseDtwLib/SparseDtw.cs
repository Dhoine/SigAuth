using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        private readonly List<string> _featureList = new List<string> { Sin, Cos, QDir, Speed };

        private (string, double, double) GetFeatureMinMaxAvg(string featureName,
            IReadOnlyList<DtwFeatures> features)
        {
            var avgMin = 0d;
            var avgMax = 0d;
            
            for (var i = 0; i < features.Count; i++)
            {
                var distances = new List<double>();

                for (var j = 0; j < features.Count; j++)
                {
                    if (i == j) continue;
                    switch (featureName)
                    {
                        case Sin:
                            distances.Add(CompareSequences(features[i].Sin, features[j].Sin, 0.5));
                            break;
                        case Cos:
                            distances.Add(CompareSequences(features[i].Cos, features[j].Cos, 0.5));
                            break;
                        case QDir:
                            distances.Add(CompareSequences(features[i].QDirs, features[j].QDirs, 0.5));
                            break;
                        case Speed:
                            distances.Add(CompareSequences(features[i].Speeds, features[j].Speeds, 0.5));
                            break;
                        default:
                            break;
                    }
                }

                avgMin += distances.Min();
                avgMax += distances.Max();
            }

            avgMin /= features.Count;
            avgMin /= features.Count;
            return (featureName, avgMin, avgMax);
        }

        public List<(string, double, double)> BuildModel(List<SignatureSampleDeserialized> origSignature)
        {
            var features = new List<DtwFeatures>();
            var extractor = new Extractor();
            
            var model = new List<(string, double, double)>();
            foreach (var sample in origSignature)
            {
                features.Add(extractor.GetDTWFeatures(sample.Sample));
            }

            foreach (var featureName in _featureList)
            {
                model.Add(GetFeatureMinMaxAvg(featureName, features));
            }

            return model;
        }

        private List<(string, double, double)> GetCheckedModel(List<SignatureSampleDeserialized> origSignature,
            RawPoint[][] checkedSample)
        {
            var features = new List<DtwFeatures>();
            var extractor = new Extractor();
            var checkedFeatures = extractor.GetDTWFeatures(checkedSample);

            var model = new List<(string, double, double)>();
            foreach (var sample in origSignature)
            {
                features.Add(extractor.GetDTWFeatures(sample.Sample));
            }
            
            foreach (var featureName in _featureList)
            {
                var distances = new List<double>();
                double sMin = 0;
                double sMax = 0;
                foreach (var feature in features)
                {
                    switch (featureName)
                    {
                        case Sin:
                            distances.Add(CompareSequences(feature.Sin, checkedFeatures.Sin, 0.5));
                            break;
                        case Cos:
                            distances.Add(CompareSequences(feature.Cos, checkedFeatures.Cos, 0.5));
                            break;
                        case QDir:
                            distances.Add(CompareSequences(feature.QDirs, checkedFeatures.QDirs, 0.5));
                            break;
                        case Speed:
                            distances.Add(CompareSequences(feature.Speeds, checkedFeatures.Speeds, 0.5));
                            break;
                        default:
                            break;
                    }
                }
                sMin = distances.Min();
                sMax = distances.Max();
                model.Add((featureName, sMin, sMax));
            }

            return model;
        }

        private List<(string, double, double)> GetDiffValues(List<(string, double, double)> featuresMinMax, List<(string, double, double)> checkedFeaturesMinMax)
        {
            var res = new List<(string, double, double)>();
            foreach (var feature in checkedFeaturesMinMax)
            {
                var correspondingFeature = featuresMinMax.First(f => f.Item1.Equals(feature.Item1));
                var diffMin = (feature.Item2 - correspondingFeature.Item2) / correspondingFeature.Item2;
                var diffMax = (feature.Item3 - correspondingFeature.Item3) / correspondingFeature.Item3;
                res.Add((feature.Item1, diffMin, diffMax));
            }

            return res;
        }

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature, RawPoint[][] checkedSample,
            List<(string, double, double)> featuresMinMax = null)
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
                total += diff.Item3 + diff.Item2;
            }
            return total < 0.06;
        }

        public double CompareSequences(List<double> s, List<double> q, double res)
        {
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
            var warpingPath = new List<int>();
            var hop = s.Count * q.Count - 1;
            warpingPath.Add(hop);
            while (hop != 0)
            {
                var lowerNeighbors = Helper.TryGetLowerNeighbors(SMMatrix, hop, s.Count);
                if (!lowerNeighbors.Any())
                    break;
                var min = lowerNeighbors.Min(n => n.Value);
                hop = lowerNeighbors.First(n => n.Value == min).Index;
                warpingPath.Add(hop);
            }

            return SMMatrix[s.Count * q.Count - 1];
        }
    }
}
