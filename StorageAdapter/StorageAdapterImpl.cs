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
        private SQLiteConnection connection;


        public StorageAdapterImpl()
        {
            Init();
        }

        public bool SaveSignatureSample(int sigId, RawPoint[][] sample)
        {
            try
            {
                if (connection.Table<Signature>().All(s => s.SignatureId != sigId))
                {
                    var sig = new Signature { SignatureId = sigId };
                    connection.Insert(sig);
                }

                int currentNum;
                var samples = connection.Table<SignatureSample>().Where(smp => smp.SignatureId == sigId);
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
                    SampleNo = currentNum
                };
                return connection.Insert(newSample) == 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public SignatureSampleDeserialized GetSignatureSample(int sigId, int sampleNo)
        {
            throw new NotImplementedException();
        }

        public int[] GetSamplesNumbersForId(int sigId)
        {
            throw new NotImplementedException();
        }

        public int[] GetSignatureIds()
        {
            throw new NotImplementedException();
        }

        public bool SetSignatureName(int sigId, string name)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSample(int sigId, int sampleNo)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSignature(int sigId)
        {
            throw new NotImplementedException();
        }

        List<SignatureSampleDeserialized> IStorageAdapter.GetAllSamples(int sigId)
        {
            throw new NotImplementedException();
        }

        private void Init()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                DbFileName);
            connection = new SQLiteConnection(dbPath);
            connection.CreateTable<SignatureSample>();
            connection.CreateTable<Signature>();
        }

        public List<SignatureSampleDeserialized> GetAllSamples(int sigId)
        {
            throw new NotImplementedException();
        }
    }
}