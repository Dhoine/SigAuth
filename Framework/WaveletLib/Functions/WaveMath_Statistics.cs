/*  Wavelet Studio Signal Processing Library - www.waveletstudio.net
    Copyright (C) 2011, 2012 Walter V. S. de Amorim - The Wavelet Studio Initiative

    Wavelet Studio is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Wavelet Studio is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System;

namespace WaveletStudio.Functions
{
    public static partial class WaveMath
    {
        /// <summary>
        ///     Calculates the mean of an array
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public static double Mean(double[] samples)
        {
            var sum = 0d;
            foreach (var value in samples) sum += value;
            return sum / samples.Length;
        }


        /// <summary>
        ///     Calculates the Probability Density Function value of a sample
        /// </summary>
        /// <param name="x"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        /// <returns></returns>
        public static double ProbabilityDensityFunction(double x, double mean, double variance)
        {
            return 1 / Math.Sqrt(2 * Math.PI * variance) * Math.Exp(-1 * (Math.Pow(x - mean, 2) / (2 * variance)));
        }

        /// <summary>
        ///     Calculates the standard deviation of an array of samples
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double StandardDeviation(double[] x)
        {
            var sum = 0d;
            var sumOfSqrs = 0d;
            foreach (var sample in x)
            {
                sum += sample;
                sumOfSqrs += Math.Pow(sample, 2);
            }

            var topSum = x.Length * sumOfSqrs - Math.Pow(sum, 2);
            return Math.Sqrt(topSum / (x.Length * (x.Length - 1)));
        }
    }
}