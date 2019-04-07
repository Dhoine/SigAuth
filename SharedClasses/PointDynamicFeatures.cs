using System.Collections.Generic;

namespace SharedClasses
{
    public class PointDynamicFeatures
    {
        public Dictionary<string, double> FeaturesDict { get; set; }

        public double this[string key]
        {
            get => GetFeature(key);
            set => AddFeature(key, value);
        }
        private void AddFeature(string key, double value)
        {
            if (FeaturesDict == null)
            {
                FeaturesDict = new Dictionary<string, double>();
            }
            FeaturesDict.Add(key, value);
        }

        private double GetFeature(string key)
        {
            return FeaturesDict[key];
        }
    }
}