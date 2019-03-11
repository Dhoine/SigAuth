using SQLite;

namespace StorageAdapter.Models
{
    public class SignatureSample
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int SignatureId { get; set; }
        public int SampleNo { get; set; }
        public string PointsSerialized { get; set; }
    }
}