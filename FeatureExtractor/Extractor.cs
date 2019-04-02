using System;
using System.Collections.Generic;
using System.Linq;
using SharedClasses;

namespace FeatureExtractor
{
    public class Extractor : IFeatureExtractor
    {
        public DtwFeatures GetDTWFeatures(RawPoint[][] sample)
        {
            var features = new DtwFeatures
            {
                Sin = new List<double>(),
                Cos = new List<double>(),
                QDirs = new List<double>(),
                Speeds = new List<double>()
            };
            foreach (var stroke in sample)
            {
                for (var i = 1; i < stroke.Length; i++)
                {
                    features.Sin.Add(Sin(stroke[i], stroke[i - 1]));
                    features.Cos.Add(Cos(stroke[i], stroke[i - 1]));
                    features.QDirs.Add(QDir(stroke[i], stroke[i - 1]));
                    features.Speeds.Add(Speed(stroke[i], stroke[i - 1]));
                }
            }
            return features;
        }

        #region Helpers

        private static double Sin(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dy / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        private static double Cos(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dx / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        private static double QDir(RawPoint current, RawPoint prev)
        {
            const int L = 16;
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var theta = Math.Atan(dy / dx);
            if (theta < (-Math.PI) / 2 + Math.PI / L || theta >= 3 * Math.PI / 2 - Math.PI / L)
            {
                return 1;
            }

            for (var i = 1; i < L; i++)
            {
                if (-Math.PI / 2 + (2 * i - 3) * Math.PI / L <= theta &&
                    theta < -Math.PI / 2 + (2 * i - 1) * Math.PI / L)
                    return i;
            }

            return 0;
        }

        private static double Speed(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var dt = current.TimeStamp - prev.TimeStamp;
            return Math.Sqrt(Math.Pow(dy, 2)+Math.Pow(dx, 2))/dt;
        }

        #endregion
    }
}