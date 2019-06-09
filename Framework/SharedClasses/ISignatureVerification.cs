using System.Collections.Generic;

namespace SharedClasses
{
    public interface ISignatureVerification
    {
        VerificationResponse CheckSignature(List<SignatureSampleDeserialized> origSignature,
            List<List<RawPoint>> checkedSample,
            SignatureModel signatureModel = null);
    }
}