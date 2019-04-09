using System;

namespace WaveletLib
{
    public static class GaussianWavelet
    {
        public static double Wavelet(double n)
        {
            return (1 - Math.Pow(n, 2)) * Math.Pow(Math.E, -Math.Pow(n, 2) / 2);
        }

        public static double DiscretizedWavelet(double n, double scale)
        {
            return (1d / scale) * Wavelet((double)n / scale);
        }
    }
}