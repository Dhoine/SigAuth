using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Accord.MachineLearning;
using SharedClasses;

namespace SparseDtwLib
{
    public class SparseDtw : ISignatureVerification
    {
        public List<string> CompareFeatureList = new List<string> { GlobalConstants.Sin, /*Cos, QDir,*/ GlobalConstants.Speed };
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
            
            foreach (var featureName in CompareFeatureList)
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

        public VerificationResponse CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample, SignatureModel signatureModel = null)
        {
            //if (signatureModel != null && signatureModel.Any())
            //{
            //    CompareFeatureList = featuresToCompare;
            //}
            //if (signatureModel == null || !signatureModel.Any())
            //{
            //    signatureModel = 
            //}
            if (CompareFeatureList == null || !CompareFeatureList.Any())
            {
                return new VerificationResponse { IsGenuine = false };
            }
            var features = new List<DtwFeatures>();

            foreach (var sample in origSignature)
            {
                features.Add(GetDTWFeatures(sample.Sample));
            }

            foreach (var feature in CompareFeatureList)
            {
                int[] outputs = new int[origSignature.Count];
                var knn = new KNearestNeighbors(origSignature.Count, new DwtComparer());
                knn.NumberOfClasses = 1;
                var inputs = features.Select(f => f.Features.Select(d => d.FeaturesDict[feature]).ToArray()).ToArray();
                knn.Learn(inputs, outputs);

                var test = GetDTWFeatures(checkedSample);
                var tst = knn.Scores(test.Features.Select(f => f.FeaturesDict[feature]).ToArray());
                if (tst.First() < 2)
                    return new VerificationResponse {IsGenuine = false};
            }
            
            //var temp = BuildModel(origSignature);
            //var model = GetCheckedModel(origSignature, checkedSample);

            //foreach (var feature in CompareFeatureList)
            //{
            //    var orig = temp.FirstOrDefault(f => f.Name == feature);
            //    var ch = model.FirstOrDefault(f => f.Name == feature);
            //    var avg = (ch.Max + ch.Min) / 2;
            //    if (avg > orig.Max)
            //        return new VerificationResponse{IsGenuine = false};
            //}

            return new VerificationResponse{IsGenuine = true};
        }

        public async Task<double> CompareSequences(IEnumerable<double> sEnumerable, IEnumerable<double> qEnumerable, double res)
        {
            var s = sEnumerable.ToList();
            var q = qEnumerable.ToList();
            var SMMatrix = Enumerable.Repeat(-1d, s.Count * q.Count).ToArray();
            Array.Clear(SMMatrix,0, s.Count * q.Count);
            var sQ = FeatureFunctions.Quantize(s);
            var qQ = FeatureFunctions.Quantize(q);
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
                        var euc = FeatureFunctions.SquareEucDist(sQ[sIndex], qQ[qIndex]);
                        SMMatrix[sQ.Count * qIndex + sIndex] = euc;
                    }
                }
            }

            for (var i = 0; i < SMMatrix.Length; i++)
            {
                if (SMMatrix[i] < 0) continue;
                var lowerNeighbors = Helper.TryGetLowerNeighbors(SMMatrix, i, sQ.Count);
                if (lowerNeighbors.Any())
                {
                    var min = lowerNeighbors.Min(n => n.Value);
                    SMMatrix[i] += min;
                }
                
                var upperNeighbors = Helper.TryGetUpperNeighbors(SMMatrix, i, sQ.Count);
                if (upperNeighbors.Any(n => n.Value >= 0)) continue;
                foreach (var neighbor in upperNeighbors)
                {
                    SMMatrix[neighbor.Index] =
                        FeatureFunctions.SquareEucDist(sQ[neighbor.Index % s.Count], qQ[neighbor.Index / s.Count]);
                }
            }

            return SMMatrix[sQ.Count * qQ.Count - 1];
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
