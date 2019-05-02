using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord;
using Accord.IO;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Markov;
using Accord.Statistics.Models.Markov.Learning;
using Accord.Statistics.Models.Markov.Topology;
using HiddenMarkovModels.MathUtils.Statistics;
using SharedClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Hmm
{

    public class HmmSignature :ISignatureVerification
    {
        private byte[,] ConvertToArray(List<List<RawPoint>> sample)
        {
            var minX = (int)sample.Select(s => s.Select(stroke => stroke.X).Min()).Min();
            var maxX = (int)sample.Select(s => s.Select(stroke => stroke.X).Max()).Max();
            var minY = (int)sample.Select(s => s.Select(stroke => stroke.Y).Min()).Min();
            var maxY = (int)sample.Select(s => s.Select(stroke => stroke.Y).Max()).Max();
            
            var image = new Image<Gray8>(maxX - minX + 1, maxY - minY + 1);
            foreach (var stroke in sample)
            {
                var linePoints = stroke.Select(s => new PointF((float)s.X - minX, (float)s.Y-minY));
                image.Mutate(i => i.DrawLines(new GraphicsOptions(true), new SolidBrush<Gray8>(new Gray8(255)), 2, linePoints.ToArray()));
            }
            var bytes = image.GetPixelSpan().ToArray().Select(p => p.PackedValue).ToArray();
            return bytes.Reshape(maxY - minY + 1, maxX - minX + 1);
        }

        private byte[][,] SplitImage(byte[,] image, bool vertical = true, bool horizontal = false)
        {
            var xSum = 0;
            var ySum = 0;
            var count = 0;
            for (int x = 0; x < image.GetLength(0); x++)
            {
                for (int y = 0; y < image.GetLength(1); y++)
                {
                    if (image[x, y] != 0)
                    {
                        xSum += x;
                        ySum += y;
                        count++;
                    }
                }
            }
            int xCenter = xSum / count;
            int yCenter = ySum / count;
            if (vertical && horizontal)
            {
                //var leftPart = image.Take(xCenter).ToList();
                //var rightPart = image.Skip(xCenter).ToList();
                //var p1 = leftPart.Select(col => col.Take(yCenter).ToArray()).ToArray().ToMatrix();
                //var p2 = rightPart.Select(col => col.Take(yCenter).ToArray()).ToArray().ToMatrix();
                //var p3 = leftPart.Select(col => col.Skip(yCenter).ToArray()).ToArray().ToMatrix();
                //var p4 = rightPart.Select(col => col.Skip(yCenter).ToArray()).ToArray().ToMatrix();
                return new[]
                {
                    image.Get(0, xCenter, 0, yCenter),
                    image.Get(xCenter, image.GetLength(0), 0, yCenter),
                    image.Get(0, xCenter, yCenter, image.GetLength(1)),
                    image.Get(xCenter, image.GetLength(0), yCenter, image.GetLength(1)),
                };
            }
            if (vertical)
            {
                return new[]
                {
                    image.Get(0, xCenter, 0, image.GetLength(1)),
                    image.Get(xCenter, image.GetLength(0),0, image.GetLength(1)),
                };
            }

            //return new[]
            //{
            //    image.Select(col => col.Take(yCenter).ToArray()).ToArray().ToMatrix(),
            //    image.Select(col => col.Skip(yCenter).ToArray()).ToArray().ToMatrix(),
            //};
            return null;
        }

        private List<int[]> GetFeatures(byte[,] image)
        {
            var initialHalfs = SplitImage(image);
            var quarters = new List<byte[,]>();
            foreach (var half in initialHalfs)
            {
                quarters.AddRange(SplitImage(half));
            }

            var parts = new List<byte[][,]>();
            foreach (var quarter in quarters)
            {
                var part2 = SplitImage(quarter, true, true);
                var part3 = part2.SelectMany(i => SplitImage(i, true, true));
                //var part4 = part3.SelectMany(i => SplitImage(i, true, true));
                parts.Add(part3.ToArray());
            }

            var coeff = new List<int[]>();
            foreach (var part in parts)
            {
                var linq = part.AsParallel().AsOrdered().Select(fr =>
                {
                    var data = fr.ToDouble();
                    CosineTransform.DCT(data);
                    return (int)data.Sum();
                });
                coeff.Add(linq.ToArray());
            }
            
            return coeff;
        }

        public VerificationResponse CheckSignature(List<SignatureSampleDeserialized> origSignature, List<List<RawPoint>> checkedSample,
            SignatureModel model)
        {
            var teachingSeq = new List<int[]>();
            foreach (var sample in origSignature)
            {
                var arr = new List<int>();
                foreach (var feature in GetFeatures(ConvertToArray(sample.Sample)))
                {
                    arr.AddRange(feature);
                }
                teachingSeq.Add(arr.ToArray());
            }

            var check = ConvertToArray(checkedSample);
            var test = GetFeatures(check);

            var hmm = new HiddenMarkovModel<NormalDistribution, double>(new Forward(4, 2), new NormalDistribution());

            var teacher = new ViterbiLearning<NormalDistribution, double>(hmm)
            {
                Tolerance = 0.00001,
                Iterations = 0,

            };

            teacher.Learn(teachingSeq.ToArray().ToDouble());
            ;
            var tst = hmm.Probability(test.ToArray().ToDouble().Flatten());
            var tst2 = hmm.Generate(6);



            return new VerificationResponse{IsGenuine = false};
        }
    }
}
