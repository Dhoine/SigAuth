using System;
using System.Collections.Generic;

namespace SparseDtwLib
{
    public class Helper
    {
        public static List<int> FindIndexes(List<double> sequence, double lowerBorder, double upperBorder)
        {
            var res = new List<int>();
            if (upperBorder < lowerBorder) throw new ArgumentException("Upper border can't be less than lower border");

            for (var i = 0; i < sequence.Count; i++)
                if (sequence[i] <= upperBorder && sequence[i] >= lowerBorder)
                    res.Add(i);

            return res;
        }

        public static List<Neighbor> TryGetLowerNeighbors(double[] array, int index, int width)
        {
            var res = new List<Neighbor>();
            var idx = index - width;
            if (idx >= 0 && array[idx] >= 0)
            {
                var lowerNeighbor = new Neighbor
                {
                    Index = idx,
                    Value = array[idx]
                };
                res.Add(lowerNeighbor);
            }

            idx = index - 1;
            if (idx >= 0 && array[idx] >= 0)
            {
                var leftNeighbor = new Neighbor
                {
                    Index = idx,
                    Value = array[idx]
                };
                res.Add(leftNeighbor);
            }

            idx = index - 1 - width;
            if (idx >= 0 && array[idx] >= 0)
            {
                var lowLeftNeighbor = new Neighbor
                {
                    Index = idx,
                    Value = array[idx]
                };
                res.Add(lowLeftNeighbor);
            }

            return res;
        }

        public static List<Neighbor> TryGetUpperNeighbors(double[] array, int index, int width)
        {
            var res = new List<Neighbor>();
            var upper = index + width;
            if (upper < array.Length)
            {
                var upperNeighbor = new Neighbor
                {
                    Index = upper,
                    Value = array[upper]
                };
                res.Add(upperNeighbor);
            }

            var right = index + 1;
            if (right < array.Length)
            {
                var rightNeighbor = new Neighbor
                {
                    Index = right,
                    Value = array[right]
                };
                res.Add(rightNeighbor);
            }

            var upperRight = index + 1 + width;
            if (upperRight >= array.Length) return res;
            var upperRightNeighbor = new Neighbor
            {
                Index = upperRight,
                Value = array[upperRight]
            };
            res.Add(upperRightNeighbor);

            return res;
        }
    }
}