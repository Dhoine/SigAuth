using System;
using System.Collections.Generic;
using System.Linq;
using DwtSig;
using EpwLib;
using SharedClasses;
using SparseDtwLib;
using StorageAdapter;

namespace IntermediateLib
{
    public class AppService : IAppService
    {
        private readonly IStorageAdapter _adapter = new StorageAdapterImpl();
        public bool TrainSignature(List<List<RawPoint>> signatureStrokes, int sigId)
        {
            return _adapter.SaveSignatureSample(sigId, signatureStrokes);
        }

        public bool CheckSignature(List<List<RawPoint>> signatureStrokes, int sigId)
        {
            var samples = _adapter.GetAllSamples(sigId);
            var sparse = new Epw();
            return sparse.CheckSignature(samples, signatureStrokes, new List<string> { GlobalConstants.Sin, GlobalConstants.Speed }, null);
            //var sample = _adapter.GetSignatureSample(sigId, 1).Sample;
            //var helper = new Epw();
            ////var test = helper.SmoothPoints(signatureStrokes);
            //var test2 = helper.GetExtremePointsUnfiltered(signatureStrokes);
            //var test3 = helper.FilterExtremePoints(test2);
            ////var test4 = helper.SmoothPoints(sample);
            //var test5 = helper.GetExtremePointsUnfiltered(sample);
            //var test6 = helper.FilterExtremePoints(test5);
            //var test7 = helper.CompareSequence(test3, test6);
            return false;
        }

        public bool DeleteSignature(int sigId)
        {
            return _adapter.DeleteSignature(sigId);
        }

        public bool DeleteSignatureSample(int sigId, int sampleNum)
        {
            return _adapter.DeleteSample(sigId, sampleNum);
        }

        public List<List<RawPoint>> GetSignaturePoints(int sigId, int sampleNo)
        {
            return _adapter.GetSignatureSample(sigId, sampleNo).Sample;
        }

        public int[] GetSavedSignaturesIds()
        {
            return _adapter.GetSignatureIds();
        }

        public int[] GetSampleNumbersForId(int sigId)
        {
            return _adapter.GetSamplesNumbersForId(sigId);
        }

        public bool SetSignatureName(int sigId, string name)
        {
            return _adapter.SetSignatureName(sigId, name);
        }

        public bool BuildSigModel(int sigId)
        {
            throw new NotImplementedException();
        }
    }
}
