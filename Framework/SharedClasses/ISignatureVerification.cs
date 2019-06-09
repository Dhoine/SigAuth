using System.Collections.Generic;

namespace SharedClasses
{
    public interface ISignatureVerification
    {
        bool CheckSignature(List<SignatureSampleDeserialized> origSignature,
            List<List<RawPoint>> checkedSample);
    }
}