using System;
using System.Collections.Generic;
using System.Linq;

namespace WaveletLib
{
    public class Dwt
    {
        public List<double> GetWaveletDecomp(double[] sequence, int scale)
        {
            var res = new List<double>();
            for (int i = 0; i < sequence.Length; i++)
            {
                res.Add(sequence[i] * GaussianWavelet.DiscretizedWavelet(i, Math.Pow(scale, 2)));
            }

            return res;
        }

        public List<List<double>> GetSameSignSubSequences(List<double> seq)
        {
            var res = new List<List<double>>();
            var indexes = new List<int>();
            var prevSign = seq.First() >= 0;
            for (int i = 1; i < seq.Count; i++)
            {
                var currSign = seq[i] >= 0;
                if (currSign != prevSign)
                    indexes.Add(i-1);
            }

            var currentPos = 0;
            foreach (var index in indexes)
            {
                var count = index - currentPos + 1;
                res.Add(seq.GetRange(currentPos, count));
                currentPos = index + 1;
            }

            return res;
        }

        public double Integral(List<double> seq)
        {
            var integral = 0d;
            for (int i = 0; i < seq.Count; i++)
            {
                var sum = seq[i];
                if (i > 0)
                    sum += seq[i - 1];
                integral += 0.5 * sum;
            }

            return integral;
        }
    }
}