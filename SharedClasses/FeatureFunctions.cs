using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedClasses
{
    public static class FeatureFunctions
    {
        public static double Sin(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dy / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        public static double Cos(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dx / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        public static double QDir(RawPoint current, RawPoint prev)
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

        public static double Speed(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var dt = current.TimeStamp - prev.TimeStamp;
            return Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2)) / dt;
        }

        public static double EucDist(double q, double p)
        {
            return Math.Pow(q - p, 2);
        }

        public static List<NameMinMax> GetDiffValues(List<NameMinMax> featuresMinMax, List<NameMinMax> checkedFeaturesMinMax)
        {
            var res = new List<NameMinMax>();
            foreach (var feature in checkedFeaturesMinMax)
            {
                var correspondingFeature = featuresMinMax.First(f => f.Name.Equals(feature.Name));
                var diffMin = (feature.Min - correspondingFeature.Min) / correspondingFeature.Min;
                var diffMax = (feature.Max - correspondingFeature.Max) / correspondingFeature.Max;
                res.Add(new NameMinMax { Name = feature.Name, Min = diffMin, Max = diffMax });
            }

            return res;
        }
    }
}