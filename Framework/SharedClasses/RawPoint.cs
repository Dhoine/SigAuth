namespace SharedClasses
{
    public class RawPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public long TimeStamp { get; set; }
        public bool IsEndOfSegment { get; set; }

        public override string ToString()
        {
            return $"X={X} Y={Y} TimeStamp={TimeStamp}";
        }
    }
}