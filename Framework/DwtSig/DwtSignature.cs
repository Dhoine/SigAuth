using System.Collections.Generic;
using System.Linq;
using MatrixLib;
using SharedClasses;
using WaveletStudio;
using WaveletStudio.Wavelet;

namespace DwtSig
{
    public class DwtSignature : ISignatureVerification
    {
        public string WaveletName { get; set; } = "db4";
        public int Level { get; set; } = 3;

        public VerificationResponse CheckSignature(List<SignatureSampleDeserialized> originalSamples,
            List<List<RawPoint>> checkedSample, SignatureModel model = null)
        {
            var origModel = BuildModel(originalSamples);
            var checkedCoeffMatrix = CoeffMatrix(FeatureFunctions.NormalizeAndFlattenSample(checkedSample));

            var distancesMin = 0d;
            var distancesMax = 0d;
            for (var i = 0; i < origModel.SamplesCoefficients.Count; i++)
            {
                var distances = new List<double>();
                for (var j = 0; j < origModel.SamplesCoefficients.Count; j++)
                {
                    if (i == j) continue;
                    var matrixOne = origModel.SamplesCoefficients[i];
                    var matrixTwo = origModel.SamplesCoefficients[j];
                    var dst = 0d;
                    for (var k = 0; k < 2; k++)
                    for (var l = 0; l < Level + 1; l++)
                        dst += FeatureFunctions.EuclideanNorm(FeatureFunctions.SubtractVectors(matrixOne[k, l],
                            matrixTwo[k, l]));
                    distances.Add(dst);
                }

                distancesMax += distances.Max();
                distancesMin += distances.Min();
            }

            var nminmax = new NameMinMax
            {
                Max = distancesMax / origModel.SamplesCoefficients.Count,
                Min = distancesMin / origModel.SamplesCoefficients.Count,
                Name = "coefs"
            };
            var comparedDistances = new List<double>();
            foreach (var t in origModel.SamplesCoefficients)
            {
                var dst = 0d;
                var matrixOne = t;

                for (var k = 0; k < 2; k++)
                for (var l = 0; l < Level + 1; l++)
                    dst += FeatureFunctions.EuclideanNorm(FeatureFunctions.SubtractVectors(matrixOne[k, l],
                        checkedCoeffMatrix[k, l]));
                comparedDistances.Add(dst);
            }

            var min = comparedDistances.Min();
            var max = comparedDistances.Max();

            return new VerificationResponse {IsGenuine = (min + max) / 2 < nminmax.Max};
        }

        public DwtFeatures BuildModel(List<SignatureSampleDeserialized> samples)
        {
            var res = new DwtFeatures
            {
                SamplesCoefficients = new List<List<double>[,]>()
            };
            foreach (var sample in samples)
            {
                var points = FeatureFunctions.NormalizeAndFlattenSample(sample.Sample);
                var coefMatrix = CoeffMatrix(points);
                res.SamplesCoefficients.Add(coefMatrix);
            }

            return res;
        }

        private List<double>[,] CoeffMatrix(List<RawPoint> points)
        {
            var x = FeatureFunctions.GetXSequence(points);
            var y = FeatureFunctions.GetYSequence(points);
            var signalX = new Signal(x);
            var signalY = new Signal(y);
            var coefMatrix = new List<double>[2, Level + 1];
            var xDtw = DWT.ExecuteDWT(signalX, MotherWavelet.LoadFromName(WaveletName), Level);
            var yDtw = DWT.ExecuteDWT(signalY, MotherWavelet.LoadFromName(WaveletName), Level);
            for (var i = 0; i < Level; i++)
            {
                coefMatrix[0, i] = xDtw[i].Details.ToList();
                coefMatrix[1, i] = yDtw[i].Details.ToList();
            }

            coefMatrix[0, Level] = xDtw[Level - 1].Approximation.ToList();
            coefMatrix[1, Level] = xDtw[Level - 1].Approximation.ToList();
            return coefMatrix;
        }
    }
}