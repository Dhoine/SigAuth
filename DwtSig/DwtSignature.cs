﻿using System;
using System.Collections.Generic;
using System.Linq;
using MatrixLib;
using SharedClasses;
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

            double distancesMin = 0d;
            double distancesMax = 0d;
            for (int i = 0; i < origModel.SamplesCoefficients.Count; i++)
            {
                var distances = new List<double>();
                for (int j = 0; j < origModel.SamplesCoefficients.Count; j++)
                {
                    if (i == j) continue;
                    var matrixOne = origModel.SamplesCoefficients[i];
                    var matrixTwo = origModel.SamplesCoefficients[j];
                    var dst = 0d;
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < Level+1; l++)
                        {
                            dst += FeatureFunctions.EuclideanNorm(FeatureFunctions.SubtractVectors(matrixOne.GetItem(k, l),
                                matrixTwo.GetItem(k, l)));
                        }
                    }
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
            for (int i = 0; i < origModel.SamplesCoefficients.Count; i++)
            {
                var dst = 0d;
                    var matrixOne = origModel.SamplesCoefficients[i];
                    
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < Level+1; l++)
                        {
                            dst += FeatureFunctions.EuclideanNorm(FeatureFunctions.SubtractVectors(matrixOne.GetItem(k, l),
                                checkedCoeffMatrix.GetItem(k, l)));
                        }
                    }
                    comparedDistances.Add(dst);
            }

            //comparedDistances /= origModel.SamplesCoefficients.Count;
            var min = comparedDistances.Min();
            var max = comparedDistances.Max();

            //var checkedMinMax = new NameMinMax {Max = max, Min = min, Name = "coefs"};
            //var diff = FeatureFunctions.GetDiffValues(new List<NameMinMax>{nminmax}, new List<NameMinMax> { checkedMinMax}).First();
            //var diffSum = diff.Min + diff.Max;
            //return diffSum < 0;
            return new VerificationResponse{IsGenuine = (min + max) / 2 < nminmax.Max };
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

        private Matrix<List<double>> CoeffMatrix(List<RawPoint> points)
        {
            var x = FeatureFunctions.GetXSequence(points);
            var y = FeatureFunctions.GetYSequence(points);
            var signalX = new WaveletStudio.Signal(x);
            var signalY = new WaveletStudio.Signal(y);
            var coefMatrix = new Matrix<List<double>>(Level+1, 2);
            var xDtw = DWT.ExecuteDWT(signalX, MotherWavelet.LoadFromName(WaveletName), Level);
            var yDtw = DWT.ExecuteDWT(signalY, MotherWavelet.LoadFromName(WaveletName), Level);
            for (var i = 0; i < Level; i++)
            {
                coefMatrix.SetItem(0, i, xDtw[i].Details.ToList());
                coefMatrix.SetItem(1, i, yDtw[i].Details.ToList());
            }

            coefMatrix.SetItem(0, Level, xDtw[Level-1].Approximation.ToList());
            coefMatrix.SetItem(1, Level, xDtw[Level-1].Approximation.ToList());
            return coefMatrix;
        }
    }
}
