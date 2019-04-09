using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses;

namespace EpwLib
{
    public class Epw
    {
        private readonly List<string> _fullFeatureList = new List<string> { GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed };
        private List<string> _compareFeatureList = new List<string> {GlobalConstants.Sin, GlobalConstants.Speed};

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample,
            List<string> featuresToCompare,
            EpwModel model)
        {
            if (featuresToCompare != null && featuresToCompare.Any())
            {
                _compareFeatureList = featuresToCompare;
            }
            if (model == null)
            {
                model = new EpwModel {Samples = new List<EpwFeature>(), MinMaxFeatures = new List<NameMinMax>()};
                foreach (var sample in origSignature)
                {
                    var points = FilterExtremePoints(GetExtremePointsUnfiltered(SmoothPoints(sample.Sample)));
                    model.Samples.Add(new EpwFeature{ExtremePoints = points});
                }

                foreach (var featureName in _fullFeatureList)
                {
                    model.MinMaxFeatures.Add(BuildMinMax(featureName, model.Samples));
                }
            }

            var checkedModel = GetCheckedModel(model.Samples, FilterExtremePoints(GetExtremePointsUnfiltered(SmoothPoints(checkedSample))));
            var diffValues = FeatureFunctions.GetDiffValues(model.MinMaxFeatures, checkedModel);
            var total = 0d;
            foreach (var diff in diffValues)
            {
                total += diff.Min + diff.Max;
            }
            return total < 0.06;
        }

        private List<NameMinMax> GetCheckedModel(List<EpwFeature> modelSamples, List<ExtremePoint> checkedSample)
        {

            var model = new List<NameMinMax>();

            foreach (var featureName in _compareFeatureList)
            {
                var distances = new List<double>();
                foreach (var sample in modelSamples)
                {
                    distances.Add(CompareSequence(sample.ExtremePoints, checkedSample, featureName));
                }
                var sMin = distances.Min();
                var sMax = distances.Max();
                model.Add(new NameMinMax { Name = featureName, Min = sMin, Max = sMax });
            }

            return model;
        }


        private NameMinMax BuildMinMax(string featureName, List<EpwFeature> features)
        { 
                var avgMin = 0d;
                var avgMax = 0d;

                for (var i = 0; i < features.Count; i++)
                {
                    var distances = new List<double>();

                    for (var j = 0; j < features.Count; j++)
                    {
                        if (i == j) continue;
                        distances.Add(CompareSequence(features[i].ExtremePoints, features[j].ExtremePoints, featureName));
                    }
                    avgMin += distances.Min();
                    avgMax += distances.Max();
                }

                avgMin /= features.Count;
                avgMax /= features.Count;
                return new NameMinMax { Name = featureName, Min = avgMin, Max = avgMax };
        }

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
                for (var i = 0; i < stroke.Count; i++)
                {
                    if (i == 0)
                    {
                        res.Add(new ExtremePoint{Point = stroke[i], Type = ExtremePointType.StartPoint});
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
                        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.EndPoint, Features = features});
                        continue;
                    }

                    var yExt = (stroke[i - 1].Y - stroke[i].Y) * (stroke[i].Y - stroke[i + 1].Y);
                    if (yExt < 0)
                    {
                        res.Add(stroke[i - 1].Y < stroke[i].Y
                            ? new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalMax, Features = features }
                            : new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalMin, Features = features });
                        continue;
                    }

                    if (yExt == 0)
                    {
                        if (stroke[i].Y < stroke[i-1].Y)
                            res.Add(new ExtremePoint{Point = stroke[i], Type = ExtremePointType.VerticalPlateauStartMin, Features = features });
                        else if (stroke[i].Y > stroke[i - 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauStartMax, Features = features });
                        else if (stroke[i].Y < stroke[i + 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauEndMin, Features = features });
                        else if (stroke[i].Y > stroke[i + 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauEndMax, Features = features });
                        continue;
                    }

                }
            }

            return res;
        }

        public List<ExtremePoint> FilterExtremePoints(List<ExtremePoint> extremePoints)
        {
            var res = new List<ExtremePoint>();
            for (int i = 0; i < extremePoints.Count; i++)
            {
                var currentPoint = extremePoints[i];
                switch (currentPoint.Type)
                {
                    case ExtremePointType.StartPoint:
                    case ExtremePointType.EndPoint:
                        continue;
                    case ExtremePointType.VerticalMin:
                    case ExtremePointType.VerticalMax:
                        res.Add(currentPoint);
                        break;
                    default:
                    {
                        var nextPoint = extremePoints[i + 1];
                        switch (currentPoint.Type)
                        {
                            case ExtremePointType.VerticalPlateauStartMax when (nextPoint.Type == ExtremePointType.VerticalPlateauEndMax || nextPoint.Type == ExtremePointType.EndPoint):
                                res.Add(new ExtremePoint
                                {
                                    Point = currentPoint.Point, Type = ExtremePointType.VerticalMax,
                                    Features = currentPoint.Features
                                });
                                continue;
                            case ExtremePointType.VerticalPlateauStartMin when (nextPoint.Type == ExtremePointType.VerticalPlateauEndMin || nextPoint.Type == ExtremePointType.EndPoint):
                                res.Add(new ExtremePoint
                                {
                                    Point = currentPoint.Point, Type = ExtremePointType.VerticalMin,
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

        public double CompareSequence(List<ExtremePoint> sample, List<ExtremePoint> reference, string featureName)
        {
            if (reference.First().Type != sample.First().Type)
            {
                reference.RemoveAt(0);
            }

            var finalWeight = double.MaxValue;
            var ewpMatrix = Enumerable.Repeat(-1d, sample.Count * reference.Count).ToArray();
            ewpMatrix[0] = FeatureFunctions.SquareEucDist(reference[0].Features[featureName], sample[0].Features[featureName]);
            ewpMatrix[reference.Count*2] = FeatureFunctions.SquareEucDist(reference[0].Features[featureName], sample[2].Features[featureName]);
            ewpMatrix[2] = FeatureFunctions.SquareEucDist(reference[2].Features[featureName], sample[0].Features[featureName]);
            for (var index = 0; index < ewpMatrix.Length; index++)
            {
                var neighborWeights = new List<double>();
                var i = index % reference.Count;
                var j = index / reference.Count;
                if (i - 1 >= 0 && j - 1 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j-1) + i - 1];
                    if (elem != -1)
                    {
                        neighborWeights.Add(elem + 0.5* FeatureFunctions.SquareEucDist(reference[i].Features[featureName], sample[j].Features[featureName]));
                    }
                }

                if (i - 1 >= 0 && j - 3 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j-3) + i-1];
                    if (elem != -1)
                    {
                        var weight = elem + FeatureFunctions.SquareEucDist(reference[i].Features[featureName], sample[j].Features[featureName]);
                        if (j - 2 >= 0)
                        {
                            weight += 2 * FeatureFunctions.SquareEucDist(sample[j - 2].Features[featureName], sample[j - 1].Features[featureName]);
                        }
                        neighborWeights.Add(weight);
                    }
                }
                if (i - 3 >= 0 && j - 1 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j - 1) + i - 3];
                    if (elem != -1)
                    {
                        var weight = elem + FeatureFunctions.SquareEucDist(reference[i].Features[featureName], sample[j].Features[featureName]);
                        if (i - 2 >= 0)
                        {
                            weight += 2 * FeatureFunctions.SquareEucDist(reference[i - 2].Features[featureName], reference[i - 1].Features[featureName]);
                        }
                        neighborWeights.Add(weight);
                    }
                }

                if (!neighborWeights.Any()) continue;
                ewpMatrix[index] = neighborWeights.Min();
                finalWeight = ewpMatrix[index];
            }

            return finalWeight;
        }   
    }
}