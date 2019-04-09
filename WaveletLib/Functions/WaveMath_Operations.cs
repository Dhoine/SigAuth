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
using System.Linq;

namespace WaveletStudio.Functions
{
    public static partial class WaveMath
    {
        /// <summary>
        /// Add two arrays
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static double[] Add(double[] array1, double[] array2)
        {
            var a1 = (double[])array2.Clone();
            var a2 = (double[])array1.Clone();
            if (a1.Length > a2.Length)
            {
                for (var i = 0; i < a2.Length; i++)
                {
                    a1[i] += a2[i];
                }
                return a1;
            }

            for (var i = 0; i < a1.Length; i++)
            {
                a2[i] += a1[i];
            }
            return a2;
        }

        /// <summary>
        /// Multiplies an array by a scalar value
        /// </summary>
        /// <param name="array"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static double[] Multiply(double[] array, double scalar)
        {
            var newArray = (double[])array.Clone();
            for (var i = 0; i < newArray.Length; i++)
            {
                newArray[i] *= scalar;
            }
            return newArray;
        }   
    }
}
