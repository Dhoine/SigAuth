﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using DwtSig;
using EpwLib;
using Hmm;
using SharedClasses;
using SparseDtwLib;
using StorageAdapter;

namespace IntermediateLib
{
    [Service]
    public class AppService
    {
        private readonly ISharedPreferences _preferences;
        private readonly StorageAdapterImpl _adapter = new StorageAdapterImpl();

        private readonly Context _context;

        public AppService(ISharedPreferences prefs)
        {
            _preferences = prefs;
        }
        public bool TrainSignature(List<List<RawPoint>> signatureStrokes, int sigId)
        {
            return _adapter.SaveSignatureSample(sigId, signatureStrokes);
        }

        public bool CheckSignature(List<List<RawPoint>> signatureStrokes, int sigId)
        {
            var samples = _adapter.GetAllSamples(sigId);
            if (!samples.Any())
                return false;
            var factory = new SignatureVerificationImplFactory();
            var impl = factory.GetSignatureVerificationImpl(_preferences);

            if (impl == null) return false;
            var oldModel = _adapter.GetModel(sigId);

            var resp = impl.CheckSignature(samples, signatureStrokes, oldModel);
            if (resp.SignatureModelUpdated)
            {
                _adapter.SaveModel(sigId, resp.NewModel);
            }

            return resp.IsGenuine;
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

        public string GetSignatureName(int sigId)
        {
            return _adapter.GetSignatureName(sigId);
        }

        public bool BuildSigModel(int sigId)
        {
            throw new NotImplementedException();
        }
    }
}
