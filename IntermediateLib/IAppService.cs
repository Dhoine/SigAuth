using System.Collections.Generic;
using SharedClasses;

namespace IntermediateLib
{
    public interface IAppService
    {
        bool TrainSignature(List<List<RawPoint>> signatureStrokes, int sigId);
        bool CheckSignature(List<List<RawPoint>> signatureStrokes, int sigId);
        bool DeleteSignature(int sigId);
        bool DeleteSignatureSample(int sigId, int sampleNum);
        List<List<RawPoint>> GetSignaturePoints(int sigId, int sigNo);
        int[] GetSavedSignaturesIds();
        int[] GetSampleNumbersForId(int sigId);
        bool SetSignatureName(int sigId, string name);
        bool BuildSigModel(int sigId);
    }
}