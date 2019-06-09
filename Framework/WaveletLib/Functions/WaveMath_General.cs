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
    /// <summary>
    ///     Common math and statistical operations
    /// </summary>
    public static partial class WaveMath
    {
        /// <summary>
        ///     Scales a number based in its minimun and maximum
        /// </summary>
        /// <param name="x"></param>
        /// <param name="currentMin"></param>
        /// <param name="currentMax"></param>
        /// <param name="newMin"></param>
        /// <param name="newMax"></param>
        /// <returns></returns>
        public static double Scale(double x, double currentMin, double currentMax, double newMin, double newMax)
        {
            return newMin + (x - currentMin) / (currentMax - currentMin) * (newMax - newMin);
        }

        /// <summary>
        ///     Decreases the sampling rate of the input by keeping every odd sample starting with the first sample.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="factor"></param>
        /// <param name="invert"></param>
        /// <returns></returns>
        public static double[] DownSample(double[] input, int factor = 2, bool invert = false)
        {
            var size = invert ? Convert.ToInt32(Math.Ceiling((double) input.Length / factor)) : input.Length / factor;
            var result = MemoryPool.Pool.New<double>(size);
            var j = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (!invert && i % factor == 0)
                    continue;
                if (invert && i % factor != 0)
                    continue;
                result[j] = input[i];
                j++;
                if (j >= result.Length)
                    break;
            }

            return result;
        }

        /// <summary>
        ///     Increases the sampling rate of the input by inserting n-1 zeros between samples.
        /// </summary>
        /// <returns></returns>
        public static double[] UpSample(double[] input, int factor = 2, bool paddRight = true)
        {
            //TODO: implement offset
            if (input == null || input.Length == 0) return new double[0];
            var size = input.Length * factor;
            var result = MemoryPool.Pool.New<double>(size - (paddRight ? factor - 1 : 0));
            for (var i = 0; i < input.Length; i++) result[i * factor] = input[i];
            return result;
        }

        /// <summary>
        ///     Returns true if the value of parameter x is a power of 2
        /// </summary>
        public static bool IsPowerOf2(int x)
        {
            return x != 0 && (x & (x - 1)) == 0;
        }


        /// <summary>
        ///     Limits the range of a number by the maximun and minimun values
        /// </summary>
        public static double LimitRange(double value, double minValue, double maxValue)
        {
            return Math.Max(minValue, Math.Min(value, maxValue));
        }
    }

    /// <summary>
    ///     FFT Computation mode
    /// </summary>
    public enum ManagedFFTModeEnum
    {
        /// <summary>
        ///     Store the trigonometric values in a table (faster)
        /// </summary>
        UseLookupTable,

        /// <summary>
        ///     Dynamicaly compute the trigonometric values (use less memory)
        /// </summary>
        DynamicTrigonometricValues
    }
}