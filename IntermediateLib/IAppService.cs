using Xamarin.Controls;

namespace IntermediateLib
{
    public interface IAppService
    {
        bool TrainSignature(RawPoint[][] signatureStrokes, int sigId);
        bool CheckSignature(RawPoint[][] signatureStrokes, int sigId);
        bool DeleteSignature(int sigId);
        RawPoint[][] GetSignaturePoints(int sigId, int sigNo);
        int[] GetSavedSignaturesIds();
        bool SetSignatureName(int sigId, string name);
        bool BuildSigModel(int sigId);
    }
}