using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using DwtSig;
using EpwLib;
using Hmm;
using SharedClasses;
using SparseDtwLib;

namespace Statistics
{
    class Program
    {
        public static Dictionary<int, List<SignatureSampleDeserialized>> Original = new Dictionary<int, List<SignatureSampleDeserialized>>();
        public static Dictionary<int, List<SignatureSampleDeserialized>> Forged = new Dictionary<int, List<SignatureSampleDeserialized>>();


        static void Main(string[] args)
        {
            ParseSignatureDb();
            Check();
        }

        static void ParseSignatureDb()
        {
            foreach (var filePath in Directory.GetFiles($"Signatures"))
            {
                var fileName = filePath.Split("\\").Last().Remove(0, 4);
                var parameters = fileName.Remove(fileName.Length - 4, 4).Split("_");
                var userId = Int32.Parse(parameters[0]);
                var num = Int32.Parse(parameters[1]);
                var lines = File.ReadAllLines(filePath);
                var buffer = new List<RawPoint>();
                var res = new List<List<RawPoint>>();
                var linesList = lines.ToList();
                linesList.RemoveAt(0);
                foreach (var line in linesList)
                {
                    var splitted = line.Split(" ");
                    if (splitted[3] == "1")
                    {
                        buffer.Add(new RawPoint{X = double.Parse(splitted[0]), Y = double.Parse(splitted[1]), TimeStamp = long.Parse(splitted[2])});
                    }
                    else
                    {
                        if (buffer.Any())
                            res.Add(buffer);
                        buffer = new List<RawPoint>();
                    }
                }

                if (buffer.Any())
                {
                    res.Add(buffer);
                }

                if (!res.Any()) continue;
                if (num <= 20)
                {
                    Original.TryAdd(userId, new List<SignatureSampleDeserialized>());
                    Original[userId].Add(new SignatureSampleDeserialized{Sample = res});
                }
                else
                {
                    Forged.TryAdd(userId, new List<SignatureSampleDeserialized>());
                    Forged[userId].Add(new SignatureSampleDeserialized { Sample = res });
                }
            }
        }

        static void Check()
        {
            var sparse = new SparseDtw
            {
                CompareFeatureList = new List<string>
                {
                    GlobalConstants.Sin, GlobalConstants.Cos, GlobalConstants.QDir, GlobalConstants.Speed
                }
            };

            var epw = new Epw();
            sparse.CompareFeatureList = new List<string> { /*GlobalConstants.Sin,*/ GlobalConstants.Cos, /*GlobalConstants.QDir,*/ GlobalConstants.Speed };

            var dwt = new DwtSignature {Level = 3, WaveletName = "db4"};

            var res = File.CreateText("results.txt");
            foreach (var key in Original.Keys)
            {
                res.WriteLine($"User #{key}:");
                var originalSignatures = GetRandomElements(Original[key], 5);
                
                var countOriginal = Original[key].Count;
                var countForged = Forged[key].Count;
                //res.WriteLine("DTW:\n");
                //CheckSignature(key, sparse, originalSignatures, countOriginal, countForged, res);
                //res.WriteLine("EPW:\n");
                //CheckSignature(key, epw, originalSignatures, countOriginal, countForged, res);
                res.WriteLine("DWT:\n");
                CheckSignature(key, dwt, originalSignatures, countOriginal, countForged, res);
            }
            res.Close();
        }

        private static void CheckSignature(int key, ISignatureVerification verification, List<SignatureSampleDeserialized> originalSignatures, int countOriginal,
            int countForged, StreamWriter writer)
        {
            var counter = 0;
            writer.WriteLine("Genuine:\n");
            foreach (var sample in Original[key])
            {
                var res = verification.CheckSignature(originalSignatures, sample.Sample, null);
                if (res.IsGenuine) counter++;
                writer.WriteLine(res.IsGenuine);
            }

            writer.WriteLine($"Success: {counter}/{countOriginal}\n");
            counter = 0;
            writer.WriteLine("Skilled Forgery:\n");
            foreach (var sample in Forged[key])
            {
                var res = verification.CheckSignature(originalSignatures, sample.Sample, null);
                if (!res.IsGenuine) counter++;
                writer.WriteLine(res.IsGenuine);
            }

            writer.WriteLine($"Success: {counter}/{countForged}\n");

            counter = 0;
            writer.WriteLine("Random Forgery:\n");
            var samples = Forged.Where(e => e.Key != key).SelectMany(el => GetRandomElements(el.Value, 5));
            foreach (var sample in samples)
            {
                var res = verification.CheckSignature(originalSignatures, sample.Sample, null);
                if (!res.IsGenuine) counter++;
                writer.WriteLine(res.IsGenuine);
            }
            writer.WriteLine($"Success: {counter}/{samples.Count()}\n");
        }

        public static List<T> GetRandomElements<T>(IEnumerable<T> list, int elementsCount)
        {
            return list.OrderBy(arg => Guid.NewGuid()).Take(elementsCount).ToList();
        }
    }
}
