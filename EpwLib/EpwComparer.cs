using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Accord.Math.Distances;
using SharedClasses;

namespace EpwLib
{
    public class EpwComparer : IDistance<ExtremePoint[]>
    {
        public string feature { get; set; }
        public double Distance(ExtremePoint[] x, ExtremePoint[] y)
        {
            return CompareSequence(x.ToList(), y.ToList(), feature);
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
            ewpMatrix[reference.Count * 2] = FeatureFunctions.SquareEucDist(reference[0].Features[featureName], sample[2].Features[featureName]);
            ewpMatrix[2] = FeatureFunctions.SquareEucDist(reference[2].Features[featureName], sample[0].Features[featureName]);
            for (var index = 0; index < ewpMatrix.Length; index++)
            {
                var neighborWeights = new List<double>();
                var i = index % reference.Count;
                var j = index / reference.Count;
                if (i - 1 >= 0 && j - 1 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j - 1) + i - 1];
                    if (elem != -1)
                    {
                        neighborWeights.Add(elem + 0.5 * FeatureFunctions.SquareEucDist(reference[i].Features[featureName], sample[j].Features[featureName]));
                    }
                }

                if (i - 1 >= 0 && j - 3 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j - 3) + i - 1];
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
            }

            var staringindex = 0;
            if (ewpMatrix[reference.Count * 2] < ewpMatrix[0] && ewpMatrix[reference.Count * 2] < ewpMatrix[2])
            {
                staringindex = reference.Count * 2;
            }

            if (ewpMatrix[2] < ewpMatrix[0] && ewpMatrix[2] < ewpMatrix[reference.Count * 2])
            {
                staringindex = 2;
            }

            var warpIndex = staringindex;
            do
            {
                var next31 = warpIndex + 3 * reference.Count + 1;
                var next11 = warpIndex + reference.Count + 1;
                var next13 = warpIndex + reference.Count + 3;

                if (next31 < ewpMatrix.Length && ewpMatrix[next31] != -1 && ewpMatrix[next31] < ewpMatrix[next11] && ewpMatrix[next31] < ewpMatrix[next13])
                {
                    warpIndex = next31;
                }
                else if (next13 < ewpMatrix.Length && ewpMatrix[next13] != -1 && ewpMatrix[next13] < ewpMatrix[next11] && (next31 >= ewpMatrix.Length || ewpMatrix[next13] < ewpMatrix[next31]))
                {
                    warpIndex = next13;
                }
                else if (next11 < ewpMatrix.Length && ewpMatrix[next11] != -1)
                {
                    warpIndex = next11;
                }
                else
                    break;

            } while (warpIndex < ewpMatrix.Length);

            var distance = ewpMatrix[warpIndex];
            return distance;
        }
    }
}