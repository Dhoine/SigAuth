namespace SharedClasses
{
    public class VerificationResponse
    {
        public bool SignatureModelUpdated { get; set; }
        public bool IsGenuine { get; set; }
        public SignatureModel NewModel { get; set; }
    }
}