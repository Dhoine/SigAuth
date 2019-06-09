using System;
using System.Collections.Generic;
using System.Linq;

namespace MatrixLib
{
    public static class MatrixHelper
    {
        public static Matrix<double> AverageMatrix(List<Matrix<double>> matrices)
        {
            var maxN = matrices.Select(m => m.N).Max();
            var maxM = matrices.Select(m => m.M).Max();
            var resultMatrix = new Matrix<double>(maxN, maxM);
            for (var i = 0; i < maxN; i++)
            for (var j = 0; j < maxM; j++)
            {
                var sum = 0d;
                var count = 0;
                foreach (var matrix in matrices)
                {
                    if (i >= matrix.N || j >= matrix.M) continue;
                    sum += matrix.GetItem(j, i);
                    count++;
                }

                resultMatrix.SetItem(j, i, sum / count);
            }

            return resultMatrix;
        }

        public static Matrix<double> SubtractMatrix(Matrix<double> a, Matrix<double> b)
        {
            var maxN = a.N > b.N ? a.N : b.N;
            var maxM = a.M > b.M ? a.M : b.M;
            ;
            var resultMatrix = new Matrix<double>(maxN, maxM);
            for (var i = 0; i < maxN; i++)
            for (var j = 0; j < maxM; j++)
                resultMatrix.SetItem(j, i, a.GetItem(j, i) - b.GetItem(j, i));

            return resultMatrix;
        }

        public static double MatrixDistance(Matrix<double> a, Matrix<double> b)
        {
            var subtr = SubtractMatrix(a, b);
            var sum = 0d;
            for (var i = 0; i < subtr.N; i++)
            for (var j = 0; j < subtr.M; j++)
                sum += Math.Pow(subtr.GetItem(j, i), 2);

            return Math.Sqrt(sum);
        }
    }
}