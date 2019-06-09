using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

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
            const int l = 16;
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var theta = Math.Atan(dy / dx);
            if (theta < -Math.PI / 2 + Math.PI / l || theta >= 3 * Math.PI / 2 - Math.PI / l) return 1;

            for (var i = 1; i < l; i++)
                if (-Math.PI / 2 + (2 * i - 3) * Math.PI / l <= theta &&
                    theta < -Math.PI / 2 + (2 * i - 1) * Math.PI / l)
                    return i;

            return 0;
        }

        public static double Speed(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var dt = current.TimeStamp - prev.TimeStamp;
            if (dt == 0)
                dt = 1;
            return Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2)) / dt;
        }

        public static double SquareEucDist(double q, double p)
        {
            return Math.Pow(q - p, 2);
        }

        public static List<RawPoint> NormalizeAndFlattenSample(List<List<RawPoint>> sample)
        {
            var maxX = sample.Select(stroke => stroke.Select(point => point.X).Max()).Max();
            var maxY = sample.Select(stroke => stroke.Select(point => point.Y).Max()).Max();
            var minX = sample.Select(stroke => stroke.Select(point => point.X).Min()).Min();
            var minY = sample.Select(stroke => stroke.Select(point => point.Y).Min()).Min();
            var res = new List<RawPoint>();
            foreach (var stroke in sample)
            foreach (var point in stroke)
            {
                var normalizedX = (point.X - minX) / (maxX - minX);
                var normalizedY = (point.Y - minY) / (maxY - minY);
                res.Add(new RawPoint {TimeStamp = point.TimeStamp, X = normalizedX, Y = normalizedY});
            }

            return res;
        }

        public static double[] GetXSequence(List<RawPoint> points)
        {
            return points.Select(p => p.X).ToArray();
        }

        public static double[] GetYSequence(List<RawPoint> points)
        {
            return points.Select(p => p.Y).ToArray();
        }

        public static List<double> SubtractVectors(List<double> a, List<double> b)
        {
            var max = a.Count > b.Count ? a.Count : b.Count;
            var res = new List<double>();
            for (var i = 0; i < max; i++)
            {
                var sum = 0d;
                if (a.Count > i) sum += a[i];
                if (b.Count > i) sum -= b[i];

                res.Add(sum);
            }

            return res;
        }

        public static double EuclideanNorm(List<double> arr)
        {
            var sum = 0d;
            foreach (var elem in arr) sum += Math.Pow(elem, 2);

            return Math.Sqrt(sum);
        }

        public static List<double> Quantize(List<double> sequence)
        {
            var max = sequence.Max();
            var min = sequence.Min();
            var res = new List<double>();
            foreach (var element in sequence) res.Add((element - min) / (max - min));

            return res;
        }

        public static byte[] ConvertToArray(List<List<RawPoint>> sample)
        {
            var minX = (int) sample.Select(s => s.Select(stroke => stroke.X).Min()).Min();
            var maxX = (int) sample.Select(s => s.Select(stroke => stroke.X).Max()).Max();
            var minY = (int) sample.Select(s => s.Select(stroke => stroke.Y).Min()).Min();
            var maxY = (int) sample.Select(s => s.Select(stroke => stroke.Y).Max()).Max();

            var image = new Image<Gray8>(maxX - minX + 1, maxY - minY + 1);
            foreach (var stroke in sample)
            {
                var linePoints = stroke.Select(s => new PointF((float) s.X - minX, (float) s.Y - minY));
                image.Mutate(i => i.DrawLines(new GraphicsOptions(true), new SolidBrush<Gray8>(new Gray8(255)), 2,
                    linePoints.ToArray()));
            }

            using (var ms = new MemoryStream())
            {
                image.SaveAsPng(ms);
                return ms.ToArray();
            }
        }
    }
}