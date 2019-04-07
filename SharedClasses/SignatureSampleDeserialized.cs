using System.Collections.Generic;

namespace SharedClasses
{
    public class SignatureSampleDeserialized
    {
        public int SigNum { get; set; }
        public int SampleNo { get; set; }
        public List<List<RawPoint>> Sample { get; set; }
    }
}