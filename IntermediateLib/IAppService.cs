using SharedClasses;

namespace IntermediateLib
{
    public interface IAppService
    {
        bool TrainSignature(RawPoint[][] signatureStrokes, int sigId);
        bool CheckSignature(RawPoint[][] signatureStrokes, int sigId);
        bool DeleteSignature(int sigId);
        bool DeleteSignatureSample(int sigId, int sampleNum);
        RawPoint[][] GetSignaturePoints(int sigId, int sigNo);
        int[] GetSavedSignaturesIds();
        int[] GetSignatureNumbersForId(int sigId);
        bool SetSignatureName(int sigId, string name);
        bool BuildSigModel(int sigId);
    }
}