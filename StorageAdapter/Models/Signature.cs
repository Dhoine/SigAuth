using System.Collections.Generic;
using SQLite;

namespace StorageAdapter.Models
{
    public class Signature
    {
        [PrimaryKey]
        public int SignatureId { get; set; }
        public string SignatureName { get; set; }
        public bool IsModelActual { get; set; }
    }
}