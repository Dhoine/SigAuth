using System;
using Xamarin.Controls;

namespace IntermediateLib
{
    public class AppService : IAppService
    {
        public bool TrainSignature(RawPoint[][] signatureStrokes, int sigId)
        {
            throw new NotImplementedException();
        }

        public bool CheckSignature(RawPoint[][] signatureStrokes, int sigId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSignature(int sigId)
        {
            throw new NotImplementedException();
        }

        public RawPoint[][] GetSignaturePoints(int sigId, int sigNo)
        {
            throw new NotImplementedException();
        }

        public int[] GetSavedSignaturesIds()
        {
            throw new NotImplementedException();
        }

        public bool SetSignatureName(int sigId, string name)
        {
            throw new NotImplementedException();
        }

        public bool BuildSigModel(int sigId)
        {
            throw new NotImplementedException();
        }
    }
}
