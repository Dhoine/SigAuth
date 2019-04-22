using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using HiddenMarkovModels;
using HiddenMarkovModels.Learning.Unsupervised;
using HiddenMarkovModels.MathUtils.Distribution;
using HiddenMarkovModels.MathUtils.Statistics;
using HiddenMarkovModels.Topology;
using SharedClasses;

namespace Hmm
{

    public class HmmSignature
    {

        public static T[,] SubArray<T>(T[,] values,
            int row_min, int row_max, int col_min, int col_max)
        {
            // Allocate the result array.
            int num_rows = row_max - row_min + 1;
            int num_cols = col_max - col_min + 1;
            T[,] result = new T[num_rows, num_cols];

            // Get the number of columns in the values array.
            int total_cols = values.GetUpperBound(1) + 1;
            int from_index = row_min * total_cols + col_min;
            int to_index = 0;
            for (int row = 0; row <= num_rows - 1; row++)
            {
                Array.Copy(values, from_index, result, to_index, num_cols);
                from_index += total_cols;
                to_index += num_cols;
            }

            return result;
        }

        private double[][] ConvertToArray(List<List<RawPoint>> sample)
        {
            var minX = (int)sample.Select(s => s.Select(stroke => stroke.X).Min()).Min();
            var maxX = (int)sample.Select(s => s.Select(stroke => stroke.X).Max()).Max();
            var minY = (int)sample.Select(s => s.Select(stroke => stroke.Y).Min()).Min();
            var maxY = (int)sample.Select(s => s.Select(stroke => stroke.Y).Max()).Max();
            
            var res = new double[maxX-minX + 1][];
            for (var i = 0; i < res.Length; i++)
            {
                res[i] = new double[maxY - minY + 1];
            }
            foreach (var stroke in sample)
            {
                foreach (var point in stroke)
                {
                    var x = (int)(point.X - minX);
                    var y = (int)(point.Y - minY);
                    res[x][y] = 1;
                }
            }

            return res;
        }

        private double[][][] SplitImage(double[][] image, bool vertical = true, bool horizontal = false)
        {
            var xSum = 0;
            var ySum = 0;
            var count = 0;
            var xCenter = 0;
            var yCenter = 0;
            for (int x = 0; x < image.Length; x++)
            {
                for (int y = 0; y < image[x].Length; y++)
                {
                    if (image[x][y] != 0)
                    {
                        xSum += x;
                        ySum += y;
                        count++;
                    }
                }
            }
            var res = new double[2][][];
            if (count == 0)
            {
                return res;
            }
            xCenter = xSum / count;
            yCenter = ySum / count;
            if (vertical && horizontal)
            {
                var leftPart = image.Take(xCenter).ToList();
                var rightPart = image.Skip(xCenter).ToList();
                var p1 = leftPart.Select(col => col.Take(yCenter).ToArray()).ToArray();
                var p2 = rightPart.Select(col => col.Take(yCenter).ToArray()).ToArray();
                var p3 = leftPart.Select(col => col.Skip(yCenter).ToArray()).ToArray();
                var p4 = rightPart.Select(col => col.Skip(yCenter).ToArray()).ToArray();
                return new[]
                {
                    p1, p2, p3, p4
                };
            }
            if (vertical)
            {
                return new[]
                {
                    image.Take(xCenter).ToArray(),
                    image.Skip(xCenter).ToArray()
                };
            }

            return new[]
            {
                image.Select(col => col.Take(yCenter).ToArray()).ToArray(),
                image.Select(col => col.Skip(yCenter).ToArray()).ToArray(),
            };
        }

        private List<double> GetFeatures(double[][] image)
        {
            var initialHalfs = SplitImage(image);
            var quarters = new List<double[][]>();
            foreach (var half in initialHalfs)
            {
                quarters.AddRange(SplitImage(half));
            }

            var parts = new List<double[][][]>();
            foreach (var quarter in quarters)
            {
                var part2 = SplitImage(quarter, true, true);
                var part3 = part2.SelectMany(i => SplitImage(i, true, true));
                var part4 = part3.SelectMany(i => SplitImage(i, true, true));
                parts.Add(part4.ToArray());
            }

            var coeff = new List<double>();
            foreach (var part in parts)
            {
                var linq = part.AsParallel().AsOrdered().Select(fr =>
                {
                    if (fr == null) return 0;
                    var data = fr.Flatten().Take(fr.Flatten().Length).ToArray();
                    CosineTransform.DCT(data);
                    return data.Sum();
                }).ToList();
                coeff.AddRange(linq);
            }

            //Parallel.ForEach(parts, fragmantLoop);

            //void fragmantLoop(double[][][] doubles)
            //{
            //    Parallel.ForEach(doubles, dctLoop);
            //}

            //void dctLoop(double[][] fragment1)
            //{
            //    if (fragment1 == null) return;
            //    var data = fragment1.Flatten().Take(fragment1.Flatten().Length).ToArray();
            //    CosineTransform.DCT(data);
            //    coeff.Add(data.Sum());
            //}
            
            return coeff;
        }

        public bool CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample,
            List<string> featuresToCompare,
            HiddenMarkovModel model)
        {
            var teachingSeq = new List<double[]>();
            foreach (var sample in origSignature)
            {
                teachingSeq.Add(GetFeatures(ConvertToArray(sample.Sample)).ToArray());
            }

            var check = ConvertToArray(checkedSample);
            var test = GetFeatures(check);

            Gaussian density = new Gaussian();
            var hmm = new HiddenMarkovModel(new Ergodic(2), density);
            var teacher = new BaumWelchLearning(hmm) { Iterations = 0, Tolerance = 0.2 };
            teacher.Run(teachingSeq.ToArray());
            double res = hmm.Evaluate(test.ToArray());
            return false;
        }
    }
}
