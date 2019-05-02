using System;
using System.Linq;
using Accord.Math.Distances;
using SharedClasses;

namespace SparseDtwLib
{
    public class DwtComparer : IDistance, IMetric<double[]>
    {
        public double Distance(double[] sEnumerable, double[] qEnumerable)
        {
            var s = sEnumerable.ToList();
            var q = qEnumerable.ToList();
            var res = 0.5;
            var SMMatrix = Enumerable.Repeat(-1d, s.Count * q.Count).ToArray();
            Array.Clear(SMMatrix, 0, s.Count * q.Count);
            var sQ = FeatureFunctions.Quantize(s);
            var qQ = FeatureFunctions.Quantize(q);
            var lowerBound = 0.0;
            var upperBound = res;
            while (lowerBound >= 0 && lowerBound <= 1 - res / 2)
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
    }
}