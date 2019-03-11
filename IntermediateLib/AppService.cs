﻿using System;
using SharedClasses;
using StorageAdapter;
using StorageAdapter = StorageAdapter.StorageAdapterImpl;

namespace IntermediateLib
{
    public class AppService : IAppService
    {
        private readonly IStorageAdapter _adapter = new StorageAdapterImpl();
        public bool TrainSignature(RawPoint[][] signatureStrokes, int sigId)
        {
            return _adapter.SaveSignatureSample(sigId, signatureStrokes);
        }

        public bool CheckSignature(RawPoint[][] signatureStrokes, int sigId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSignature(int sigId)
        {
            return _adapter.DeleteSignature(sigId);
        }

        public bool DeleteSignatureSample(int sigId, int sampleNum)
        {
            return _adapter.DeleteSample(sigId, sampleNum);
        }

        public RawPoint[][] GetSignaturePoints(int sigId, int sampleNo)
        {
            return _adapter.GetSignatureSample(sigId, sampleNo).Sample;
        }

        public int[] GetSavedSignaturesIds()
        {
            return _adapter.GetSignatureIds();
        }

        public int[] GetSignatureNumbersForId(int sigId)
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
