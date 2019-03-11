using System.Collections.Generic;
using SharedClasses;

namespace StorageAdapter
{
    public interface IStorageAdapter
    {
        bool SaveSignatureSample(int sigId, RawPoint[][] sample);
        SignatureSampleDeserialized GetSignatureSample(int sigId, int sampleNo);
        List<SignatureSampleDeserialized> GetAllSamples(int sigId);
        int[] GetSamplesNumbersForId(int sigId);
        int[] GetSignatureIds();
        bool SetSignatureName(int sigId, string name);
        bool DeleteSample(int sigId, int sampleNo);
        bool DeleteSignature(int sigId);
    }
}