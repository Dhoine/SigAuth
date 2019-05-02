using System.Collections.Generic;
using System.Linq;
using Android.Content;
using DwtSig;
using EpwLib;
using Hmm;
using SharedClasses;
using SparseDtwLib;

namespace IntermediateLib
{
    public class SignatureVerificationImplFactory
    {
        public ISignatureVerification GetSignatureVerificationImpl(ISharedPreferences preferences)
        {
            var method = int.Parse(preferences.GetString("verification_method", "1"));
            ISignatureVerification impl = null;
            switch (method)
            {
                case 1:
                    impl = new SparseDtw
                    {
                        CompareFeatureList = preferences.GetStringSet("dtw_features", new List<string>()).ToList()
                    };
                    break;
                case 2:
                    impl = new Epw
                    {
                        _compareFeatureList = preferences.GetStringSet("epw_features", new List<string>()).ToList()
                    };
                    break;
                case 3:
                    impl = new DwtSignature
                    {
                        WaveletName = preferences.GetString("wavelet_type", "db4"),
                        Level = preferences.GetInt("wavelet_level", 3)
                    };
                    break;
                case 4:
                    impl = new HmmSignature();
                    break;
            }

            return impl;
        }
    }
}