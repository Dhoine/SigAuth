using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SharedClasses;

namespace EpwLib
{
    public class Epw : ISignatureVerification
    {
        private readonly List<string> _fullFeatureList = new List<string>
            {GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed};

        public List<string> CompareFeatureList = new List<string>
            {GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed};

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature,
            List<List<RawPoint>> checkedSample
        )
        {
            var model = new EpwModel {Samples = new List<EpwFeature>(), MinMaxFeatures = new List<NameMinMax>()};
            foreach (var sample in origSignature)
            {
                var points = FilterExtremePoints(GetExtremePointsUnfiltered(SmoothPoints(sample.Sample)));
                model.Samples.Add(new EpwFeature {ExtremePoints = points});
            }

            foreach (var featureName in _fullFeatureList)
                model.MinMaxFeatures.Add(BuildMinMax(featureName, model.Samples));

            var checkedModel = GetCheckedModel(model.Samples,
                FilterExtremePoints(GetExtremePointsUnfiltered(SmoothPoints(checkedSample))));

            foreach (var feature in CompareFeatureList)
            {
                var orig = model.MinMaxFeatures.FirstOrDefault(f => f.Name == feature);
                var ch = checkedModel.FirstOrDefault(f => f.Name == feature);
                var avg = (ch.Max + ch.Min) / 2;
                if (avg > orig.Max)
                    return true;
            }

            return true;
        }

        private List<NameMinMax> GetCheckedModel(List<EpwFeature> modelSamples, List<ExtremePoint> checkedSample)
        {
            var model = new List<NameMinMax>();

            foreach (var featureName in CompareFeatureList)
            {
                var distances = new List<double>();
                foreach (var sample in modelSamples)
                    distances.Add(CompareSequence(sample.ExtremePoints, checkedSample, featureName));
                var sMin = distances.Min();
                var sMax = distances.Max();
                model.Add(new NameMinMax {Name = featureName, Min = sMin, Max = sMax});
            }

            return model;
        }


        private NameMinMax BuildMinMax(string featureName, List<EpwFeature> features)
        {
            var avgMin = 0d;
            var avgMax = 0d;

            for (var i = 0; i < features.Count; i++)
            {
                var distances = features.Where((t, j) => i != j)
                    .Select(t => CompareSequence(features[i].ExtremePoints, t.ExtremePoints, featureName)).ToList();

                avgMin += distances.Min();
                avgMax += distances.Max();
            }

            avgMin /= features.Count;
            avgMax /= features.Count;
            return new NameMinMax {Name = featureName, Min = avgMin, Max = avgMax};
        }

        #region PointsExtraction

        public List<List<RawPoint>> SmoothPoints(List<List<RawPoint>> origSample)
        {
            var res = new List<List<RawPoint>>();
            foreach (var stroke in origSample)
            {
                var smoothedStroke = new List<RawPoint>();
                for (var i = 0; i < stroke.Count; i++)
                    if (i == 0 || i == stroke.Count - 1)
                    {
                        smoothedStroke.Add(stroke[i]);
                    }
                    else
                    {
                        var smoothedPoint = new RawPoint
                        {
                            TimeStamp = stroke[i].TimeStamp,
                            X = Math.Ceiling(0.25 * stroke[i - 1].X + 0.5 * stroke[i].X + 0.25 * stroke[i + 1].X),
                            Y = Math.Ceiling(0.25 * stroke[i - 1].Y + 0.5 * stroke[i].Y + 0.25 * stroke[i + 1].Y)
                        };
                        smoothedStroke.Add(smoothedPoint);
                    }

                res.Add(smoothedStroke);
            }

            return res;
        }

        public List<ExtremePoint> GetExtremePointsUnfiltered(List<List<RawPoint>> sample)
        {
            var res = new List<ExtremePoint>();
            foreach (var stroke in sample)
            {
                if (stroke.Count <= 1) continue;
                var tempRes = new List<ExtremePoint>();
                for (var i = 0; i < stroke.Count; i++)
                {
                    if (i == 0)
                    {
                        tempRes.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.StartPoint });
                        continue;
                    }

                    var features = new PointDynamicFeatures
                    {
                        [GlobalConstants.Sin] = FeatureFunctions.Sin(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Speed] = FeatureFunctions.Speed(stroke[i], stroke[i - 1]),
                        [GlobalConstants.Cos] = FeatureFunctions.Cos(stroke[i], stroke[i - 1]),
                        [GlobalConstants.QDir] = FeatureFunctions.QDir(stroke[i], stroke[i - 1])
                    };

                    if (i == stroke.Count - 1)
                    {
                        tempRes.Add(new ExtremePoint
                        { Point = stroke[i], Type = ExtremePointType.EndPoint, Features = features });
                        continue;
                    }

                    var yExt = (stroke[i - 1].Y - stroke[i].Y) * (stroke[i].Y - stroke[i + 1].Y);
                    if (yExt < 0)
                    {
                        tempRes.Add(stroke[i - 1].Y < stroke[i].Y
                            ? new ExtremePoint
                            { Point = stroke[i], Type = ExtremePointType.VerticalMax, Features = features }
                            : new ExtremePoint
                            { Point = stroke[i], Type = ExtremePointType.VerticalMin, Features = features });
                        continue;
                    }

                    if (yExt == 0)
                    {
                        if (stroke[i].Y < stroke[i - 1].Y)
                            tempRes.Add(new ExtremePoint
                            {
                                Point = stroke[i],
                                Type = ExtremePointType.VerticalPlateauStartMin,
                                Features = features
                            });
                        else if (stroke[i].Y > stroke[i - 1].Y)
                            tempRes.Add(new ExtremePoint
                            {
                                Point = stroke[i],
                                Type = ExtremePointType.VerticalPlateauStartMax,
                                Features = features
                            });
                        else if (stroke[i].Y < stroke[i + 1].Y)
                            tempRes.Add(new ExtremePoint
                            {
                                Point = stroke[i],
                                Type = ExtremePointType.VerticalPlateauEndMin,
                                Features = features
                            });
                        else if (stroke[i].Y > stroke[i + 1].Y)
                            tempRes.Add(new ExtremePoint
                            {
                                Point = stroke[i],
                                Type = ExtremePointType.VerticalPlateauEndMax,
                                Features = features
                            });
                    }
                }

                tempRes[0].Features = new PointDynamicFeatures
                {
                    [GlobalConstants.Sin] = FeatureFunctions.Sin(stroke[1], stroke[0]),
                    [GlobalConstants.Speed] = FeatureFunctions.Speed(stroke[1], stroke[0]),
                    [GlobalConstants.Cos] = FeatureFunctions.Cos(stroke[1], stroke[0]),
                    [GlobalConstants.QDir] = FeatureFunctions.QDir(stroke[1], stroke[0])
                };
                ;
                res.AddRange(tempRes);
            }

            return res;
        }

        public List<ExtremePoint> FilterExtremePoints(List<ExtremePoint> extremePoints)
        {
            var res = new List<ExtremePoint>();
            for (var i = 0; i < extremePoints.Count; i++)
            {
                var currentPoint = extremePoints[i];
                switch (currentPoint.Type)
                {
                    case ExtremePointType.StartPoint:
                        if (extremePoints[i + 1].Point.Y > currentPoint.Point.Y)
                            res.Add(new ExtremePoint
                            {
                                Point = extremePoints[i].Point,
                                Type = ExtremePointType.VerticalMin,
                                Features = extremePoints[i].Features
                            });
                        else
                            res.Add(new ExtremePoint
                            {
                                Point = extremePoints[i].Point,
                                Type = ExtremePointType.VerticalMax,
                                Features = extremePoints[i].Features
                            });
                        break;
                    case ExtremePointType.EndPoint:
                        if (extremePoints[i - 1].Point.Y > currentPoint.Point.Y)
                            res.Add(new ExtremePoint
                            {
                                Point = extremePoints[1].Point,
                                Type = ExtremePointType.VerticalMin,
                                Features = extremePoints[1].Features
                            });
                        else
                            res.Add(new ExtremePoint
                            {
                                Point = extremePoints[i].Point,
                                Type = ExtremePointType.VerticalMax,
                                Features = extremePoints[i].Features
                            });
                        break;
                    case ExtremePointType.VerticalMin:
                    case ExtremePointType.VerticalMax:
                        res.Add(currentPoint);
                        break;
                    default:
                        {
                            var nextPoint = extremePoints[i + 1];
                            switch (currentPoint.Type)
                            {
                                case ExtremePointType.VerticalPlateauStartMax
                                    when nextPoint.Type == ExtremePointType.VerticalPlateauEndMax ||
                                         nextPoint.Type == ExtremePointType.EndPoint:
                                    res.Add(new ExtremePoint
                                    {
                                        Point = currentPoint.Point,
                                        Type = ExtremePointType.VerticalMax,
                                        Features = currentPoint.Features
                                    });
                                    continue;
                                case ExtremePointType.VerticalPlateauStartMin
                                    when nextPoint.Type == ExtremePointType.VerticalPlateauEndMin ||
                                         nextPoint.Type == ExtremePointType.EndPoint:
                                    res.Add(new ExtremePoint
                                    {
                                        Point = currentPoint.Point,
                                        Type = ExtremePointType.VerticalMin,
                                        Features = currentPoint.Features
                                    });
                                    continue;
                            }

                            break;
                        }
                }
            }

            return res;
        }

        #endregion

        #region CompareSequences

        public double CompareSequence(List<ExtremePoint> sample, List<ExtremePoint> reference, string featureName)
        {
            if (reference.First().Type != sample.First().Type) reference.RemoveAt(0);

            var ewpMatrix = Enumerable.Repeat(-1d, sample.Count * reference.Count).ToArray();
            ewpMatrix[0] =
                FeatureFunctions.SquareEucDist(reference[0].Features[featureName], sample[0].Features[featureName]);
            ewpMatrix[reference.Count * 2] = FeatureFunctions.SquareEucDist(reference[0].Features[featureName],
                sample[2].Features[featureName]);
            ewpMatrix[2] =
                FeatureFunctions.SquareEucDist(reference[2].Features[featureName], sample[0].Features[featureName]);
            for (var index = 0; index < ewpMatrix.Length; index++)
            {
                var neighborWeights = new List<double>();
                var i = index % reference.Count;
                var j = index / reference.Count;
                Extract11Neighbor(sample, reference, featureName, i, j, ewpMatrix, neighborWeights);

                Extract13Neighbor(sample, reference, featureName, i, j, ewpMatrix, neighborWeights);

                Extract31Neighbor(sample, reference, featureName, i, j, ewpMatrix, neighborWeights);

                if (!neighborWeights.Any()) continue;
                ewpMatrix[index] = neighborWeights.Min();
            }

            return FindWarpingPath(reference, ewpMatrix);
        }

        private static double FindWarpingPath(ICollection reference, IReadOnlyList<double> ewpMatrix)
        {
            var staringIndex = 0;
            if (ewpMatrix[reference.Count * 2] < ewpMatrix[0] && ewpMatrix[reference.Count * 2] < ewpMatrix[2])
                staringIndex = reference.Count * 2;

            if (ewpMatrix[2] < ewpMatrix[0] && ewpMatrix[2] < ewpMatrix[reference.Count * 2]) staringIndex = 2;

            var warpIndex = staringIndex;
            do
            {
                var next31 = warpIndex + 3 * reference.Count + 1;
                var next11 = warpIndex + reference.Count + 1;
                var next13 = warpIndex + reference.Count + 3;

                if (next31 < ewpMatrix.Count && ewpMatrix[next31] >= 0 && ewpMatrix[next31] < ewpMatrix[next11] &&
                    ewpMatrix[next31] < ewpMatrix[next13])
                    warpIndex = next31;
                else if (next13 < ewpMatrix.Count && ewpMatrix[next13] >= 0 &&
                         ewpMatrix[next13] < ewpMatrix[next11] &&
                         (next31 >= ewpMatrix.Count || ewpMatrix[next13] < ewpMatrix[next31]))
                    warpIndex = next13;
                else if (next11 < ewpMatrix.Count && ewpMatrix[next11] >= 0)
                    warpIndex = next11;
                else
                    break;
            } while (warpIndex < ewpMatrix.Count);


            return ewpMatrix[warpIndex];
        }

        private static void Extract31Neighbor(IReadOnlyList<ExtremePoint> sample, IReadOnlyList<ExtremePoint> reference, string featureName, int i, int j, IReadOnlyList<double> ewpMatrix,
            ICollection<double> neighborWeights)
        {
            if (i - 3 < 0 || j - 1 < 0) return;
            var elem = ewpMatrix[reference.Count * (j - 1) + i - 3];
            if (elem < 0) return;
            var weight = elem + FeatureFunctions.SquareEucDist(reference[i].Features[featureName],
                             sample[j].Features[featureName]);
            if (i - 2 >= 0)
                weight += 2 * FeatureFunctions.SquareEucDist(reference[i - 2].Features[featureName],
                              reference[i - 1].Features[featureName]);
            neighborWeights.Add(weight);
        }

        private static void Extract13Neighbor(IReadOnlyList<ExtremePoint> sample, IReadOnlyList<ExtremePoint> reference, string featureName, int i, int j, IReadOnlyList<double> ewpMatrix,
            ICollection<double> neighborWeights)
        {
            if (i - 1 < 0 || j - 3 < 0) return;
            var elem = ewpMatrix[reference.Count * (j - 3) + i - 1];
            if (elem < 0) return;
            var weight = elem + FeatureFunctions.SquareEucDist(reference[i].Features[featureName],
                             sample[j].Features[featureName]);
            if (j - 2 >= 0)
                weight += 2 * FeatureFunctions.SquareEucDist(sample[j - 2].Features[featureName],
                              sample[j - 1].Features[featureName]);

            neighborWeights.Add(weight);
        }

        private static void Extract11Neighbor(List<ExtremePoint> sample, List<ExtremePoint> reference, string featureName, int i, int j, double[] ewpMatrix,
            List<double> neighborWeights)
        {
            if (i - 1 < 0 || j - 1 < 0) return;
            var elem = ewpMatrix[reference.Count * (j - 1) + i - 1];
            if (elem >= 0)
                neighborWeights.Add(elem + 0.5 *
                                    FeatureFunctions.SquareEucDist(reference[i].Features[featureName],
                                        sample[j].Features[featureName]));
        }

        #endregion
    }
}