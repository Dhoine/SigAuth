using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharedClasses;
using SQLite;
using StorageAdapter.Models;

namespace StorageAdapter
{
    public class StorageAdapterImpl : IStorageAdapter
    {
        private const string DbFileName = "signatures.db3";
        private SQLiteConnection _connection;


        public StorageAdapterImpl()
        {
            Init();
        }

        public bool SaveSignatureSample(int sigId, List<List<RawPoint>> sample)
        {
            try
            {
                if (_connection.Table<Signature>().All(s => s.SignatureId != sigId))
                {
                    var sig = new Signature { SignatureId = sigId, IsModelActual = false};
                    _connection.Insert(sig);
                }
                else
                {
                    var sig = _connection.Get<Signature>(sigId);
                    sig.IsModelActual = false;
                    _connection.Update(sig);
                }

                int currentNum;
                var samples = _connection.Table<SignatureSample>().Where(smp => smp.SignatureId == sigId);
                if (samples.Any())
                {
                    currentNum = samples.Max((smp => smp.SampleNo)) + 1;
                }
                else
                {
                    currentNum = 0;
                }

                var newSample = new SignatureSample
                {
                    PointsSerialized = JsonConvert.SerializeObject(sample),
                    SampleNo = currentNum,
                    SignatureId = sigId
                };
                return _connection.Insert(newSample) == 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public SignatureSampleDeserialized GetSignatureSample(int sigId, int sampleNo)
        {
            var ret = new SignatureSampleDeserialized();
            var smpl = _connection.Table<SignatureSample>()
                .SingleOrDefault(sample => sample.SignatureId == sigId && sample.SampleNo == sampleNo);
            ret.Sample = smpl == null ? null : JsonConvert.DeserializeObject<List<List<RawPoint>>>(smpl.PointsSerialized);
            ret.SigNum = sigId;
            ret.SampleNo = sampleNo;
            return ret;
        }

        public int[] GetSamplesNumbersForId(int sigId)
        {
            var samples = _connection.Table<SignatureSample>().Where(s => s.SignatureId == sigId).Select(s => s.SampleNo);
            return samples.ToArray();
        }

        public int[] GetSignatureIds()
        {
            return _connection.Table<Signature>().Select(s => s.SignatureId).ToArray();
        }

        public bool SetSignatureName(int sigId, string name)
        {
            var sig = _connection.Table<Signature>().SingleOrDefault(s => s.SignatureId == sigId);
            if (sig == null)
                return false;
            sig.SignatureName = name;
            _connection.Update(sig);
            return true;
        }

        public bool DeleteSample(int sigId, int sampleNo)
        {
            var sig = _connection.Get<Signature>(sigId);
            sig.IsModelActual = false;
            _connection.Update(sig);
            var sample = _connection.Table<SignatureSample>()
                .FirstOrDefault(s => s.SampleNo == sampleNo && s.SignatureId == sigId);
            if (sample == null)
                return false;
            _connection.Delete(sample);
            return true;
        }

        public bool DeleteSignature(int sigId)
        {
            var samples = _connection.Table<SignatureSample>()
                .Where(s => s.SignatureId == sigId);
            foreach (var sample in samples)
            {
                _connection.Delete(sample);
            }

            if (_connection.Table<Signature>().All(s => s.SignatureId != sigId)) return false;
            _connection.Delete<Signature>(sigId);
            return true;

        }

        public List<SignatureSampleDeserialized> GetAllSamples(int sigId)
        {
            return _connection.Table<SignatureSample>()
                .Where(s => s.SignatureId == sigId).Select( sample => new SignatureSampleDeserialized
                {
                    Sample = JsonConvert.DeserializeObject<List<List<RawPoint>>>(sample.PointsSerialized),
                    SampleNo = sample.SampleNo,
                    SigNum = sigId
                }).ToList();
        }

        private void Init()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                DbFileName);
            _connection = new SQLiteConnection(dbPath);
            _connection.CreateTable<SignatureSample>();
            _connection.CreateTable<Signature>();
        }
    }
}