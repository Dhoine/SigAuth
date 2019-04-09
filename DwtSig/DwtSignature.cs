using System;
using System.Collections.Generic;
using System.Linq;
using MatrixLib;
using SharedClasses;
using WaveletStudio.Wavelet;

namespace DwtSig
{
    public class DwtSignature
    {
        private readonly List<string> _fullFeatureList = new List<string> { GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed };
        private List<string> _compareFeatureList = new List<string> { GlobalConstants.Sin, GlobalConstants.Speed };
        public bool CheckSignature(List<SignatureSampleDeserialized> originalSamples,
            List<List<RawPoint>> checkedSample)
        {
            var origModel = BuildModel(originalSamples);
            var checkedCoeffMatrix = CoeffMatrix(FeatureFunctions.NormalizeAndFlattenSample(checkedSample));
            var distance = 0d;
            foreach (var matrix in origModel.SamplesCoefficients)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        distance += FeatureFunctions.EuclideanNorm(FeatureFunctions.SubtractVectors(matrix.GetItem(i, j),
                            checkedCoeffMatrix.GetItem(i, j)));
                    }
                }
            }
            return false;
        }

        public DwtFeatures BuildModel(List<SignatureSampleDeserialized> samples)
        {
            var res = new DwtFeatures
            {
                SamplesCoefficients = new List<Matrix<List<double>>>()
            };
            foreach (var sample in samples)
            {
                var points = FeatureFunctions.NormalizeAndFlattenSample(sample.Sample);
                var coefMatrix = CoeffMatrix(points);
                res.SamplesCoefficients.Add(coefMatrix);
            }

            return res;
        }

        private static Matrix<List<double>> CoeffMatrix(List<RawPoint> points)
        {
            var x = FeatureFunctions.GetXSequence(points);
            var y = FeatureFunctions.GetYSequence(points);
            var signalX = new WaveletStudio.Signal(x);
            var signalY = new WaveletStudio.Signal(y);
            var coefMatrix = new Matrix<List<double>>(4, 2);
            var xDtw = DWT.ExecuteDWT(signalX, MotherWavelet.LoadFromName("db4"), 3);
            var yDtw = DWT.ExecuteDWT(signalY, MotherWavelet.LoadFromName("db4"), 3);
            for (var i = 0; i < 3; i++)
            {
                coefMatrix.SetItem(0, i, xDtw[i].Details.ToList());
                coefMatrix.SetItem(1, i, yDtw[i].Details.ToList());
            }

            coefMatrix.SetItem(0, 3, xDtw[2].Approximation.ToList());
            coefMatrix.SetItem(1, 3, xDtw[2].Approximation.ToList());
            return coefMatrix;
        }
    }
}
