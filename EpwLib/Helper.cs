using System;
using System.Collections.Generic;
using System.Linq;
using SharedClasses;

namespace EpwLib
{
    public class Helper
    {
        private int plateauLengthThreshold = 5;
        public List<List<RawPoint>> SmoothPoints(List<List<RawPoint>> origSample)
        {
            var res = new List<List<RawPoint>>();
            foreach (var stroke in origSample)
            {
                var smoothedStroke = new List<RawPoint>();
                for (var i = 0; i < stroke.Count; i++)
                    if (i == 0 || i == stroke.Count - 1)
                    {
                        smoothedStroke.Add(stroke[i]);
                    }
                    else
                    {
                        var smoothedPoint = new RawPoint
                        {
                            TimeStamp = stroke[i].TimeStamp,
                            X = Math.Ceiling(0.25 * stroke[i - 1].X + 0.5 * stroke[i].X + 0.25 * stroke[i + 1].X),
                            Y = Math.Ceiling(0.25 * stroke[i - 1].Y + 0.5 * stroke[i].Y + 0.25 * stroke[i + 1].Y)
                        };
                        smoothedStroke.Add(smoothedPoint);
                    }

                res.Add(smoothedStroke);
            }

            return res;
        }

        public List<ExtremePoint> GetExtremePointsUnfiltered(List<List<RawPoint>> sample)
        {
            var res = new List<ExtremePoint>();
            foreach (var stroke in sample)
            {
                //var prevSign = false;
                for (var i = 0; i < stroke.Count; i++)
                {
                    if (i == 0)
                    {
                        res.Add(new ExtremePoint{Point = stroke[i], Type = ExtremePointType.StartPoint});
                        continue;
                    }
                    var sin = Sin(stroke[i], stroke[i - 1]);
                    var speed = Speed(stroke[i], stroke[i - 1]);
                    if (i == stroke.Count - 1)
                    {
                        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.EndPoint, Speed = speed, Sin = sin});
                        continue;
                    }

                    
                    //var xExt = (stroke[i - 1].X - stroke[i].X) * (stroke[i].X - stroke[i + 1].X);
                    //if (xExt < 0)
                    //{
                    //    res.Add(stroke[i - 1].X < stroke[i].X
                    //        ? new ExtremePoint {Point = stroke[i], Type = ExtremePointType.HorizontalMax}
                    //        : new ExtremePoint {Point = stroke[i], Type = ExtremePointType.HorizontalMin});
                    //    continue;
                    //}

                    //if (xExt == 0)
                    //{
                    //    if (stroke[i].X < stroke[i - 1].X)
                    //        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.HorizontalPlateauStartMin });
                    //    else if (stroke[i].X > stroke[i - 1].X)
                    //        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.HorizontalPlateauStartMax });
                    //    else if (stroke[i].X < stroke[i + 1].X)
                    //        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.HorizontalPlateauEndMin });
                    //    else if (stroke[i].X > stroke[i + 1].X)
                    //        res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.HorizontalPlateauEndMax });
                    //    continue;
                    //}

                    var yExt = (stroke[i - 1].Y - stroke[i].Y) * (stroke[i].Y - stroke[i + 1].Y);
                    if (yExt < 0)
                    {
                        res.Add(stroke[i - 1].Y < stroke[i].Y
                            ? new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalMax, Speed = speed, Sin = sin }
                            : new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalMin, Speed = speed, Sin = sin });
                        continue;
                    }

                    if (yExt == 0)
                    {
                        if (stroke[i].Y < stroke[i-1].Y)
                            res.Add(new ExtremePoint{Point = stroke[i], Type = ExtremePointType.VerticalPlateauStartMin, Speed = speed, Sin = sin });
                        else if (stroke[i].Y > stroke[i - 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauStartMax, Speed = speed, Sin = sin });
                        else if (stroke[i].Y < stroke[i + 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauEndMin, Speed = speed, Sin = sin });
                        else if (stroke[i].Y > stroke[i + 1].Y)
                            res.Add(new ExtremePoint { Point = stroke[i], Type = ExtremePointType.VerticalPlateauEndMax, Speed = speed, Sin = sin });
                        continue;
                    }

                    //if (i < 2 || i > stroke.Count - 3)
                    //    continue;
                    //if (i == 2)
                    //{
                    //    prevSign = (stroke[i - 2].X - stroke[i].X) * (stroke[i + 2].Y - stroke[i].Y) -
                    //               (stroke[i - 2].Y - stroke[i].Y) * (stroke[i + 2].X - stroke[i].X) > 0;
                    //    continue;
                    //}

                    //var currSign = (stroke[i - 2].X - stroke[i].X) * (stroke[i + 2].Y - stroke[i].Y) -
                    //               (stroke[i - 2].Y - stroke[i].Y) * (stroke[i + 2].X - stroke[i].X) > 0;
                    //if (prevSign != currSign) res.Add(new ExtremePoint {Point = stroke[i], Type = ExtremePointType.Inflection});

                    //prevSign = currSign;
                }
            }

            return res;
        }

        public List<ExtremePoint> FilterExtremePoints(List<ExtremePoint> extremePoints)
        {
            var res = new List<ExtremePoint>();
            for (int i = 0; i < extremePoints.Count; i++)
            {
                var currentPoint = extremePoints[i];
                if (currentPoint.Type == ExtremePointType.StartPoint || currentPoint.Type == ExtremePointType.EndPoint)
                    continue;
                if (currentPoint.Type == ExtremePointType.HorizontalMax || currentPoint.Type == ExtremePointType.HorizontalMin || currentPoint.Type == ExtremePointType.VerticalMin || currentPoint.Type == ExtremePointType.VerticalMax)
                    res.Add(currentPoint);
                else
                {
                    var nextPoint = extremePoints[i + 1];
                    switch (currentPoint.Type)
                    {
                        case ExtremePointType.VerticalPlateauStartMax when nextPoint.Type == ExtremePointType.VerticalPlateauEndMax && plateauLengthThreshold > Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            res.Add(new ExtremePoint{Point = currentPoint.Point, Type = ExtremePointType.VerticalMax, Sin = currentPoint.Sin, Speed = currentPoint.Speed});
                            continue;
                        case ExtremePointType.VerticalPlateauStartMin when nextPoint.Type == ExtremePointType.VerticalPlateauEndMin && plateauLengthThreshold > Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMin, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMax when nextPoint.Type == ExtremePointType.VerticalPlateauEndMax && plateauLengthThreshold <=
                                                                           Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            //res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMax, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            //res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMin when nextPoint.Type == ExtremePointType.VerticalPlateauEndMin && plateauLengthThreshold <=
                                                                           Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            //res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMin, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            //res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMin when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold > Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMin, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMax when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold > Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMax, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMin when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold <= Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            //res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMin, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            continue;
                        case ExtremePointType.VerticalPlateauStartMax when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold <= Math.Abs(currentPoint.Point.X - nextPoint.Point.X):
                            //res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                            res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.VerticalMax, Sin = currentPoint.Sin, Speed = currentPoint.Speed });
                            continue;
                            /////
                        //case ExtremePointType.HorizontalPlateauStartMax when nextPoint.Type == ExtremePointType.HorizontalPlateauEndMax && plateauLengthThreshold > Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMax });
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMin when nextPoint.Type == ExtremePointType.HorizontalPlateauEndMin && plateauLengthThreshold > Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMin });
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMax when nextPoint.Type == ExtremePointType.HorizontalPlateauEndMax && plateauLengthThreshold <=
                        //                                                   Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMax });
                        //    res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMin when nextPoint.Type == ExtremePointType.HorizontalPlateauEndMin && plateauLengthThreshold <=
                        //                                                   Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMin });
                        //    res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMin when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold > Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMax when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold > Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMin when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold <= Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMin });
                        //    continue;
                        //case ExtremePointType.HorizontalPlateauStartMax when nextPoint.Type == ExtremePointType.EndPoint && plateauLengthThreshold <= Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y):
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                        //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.HorizontalMax });
                        //    continue;
                    }

                    if ((currentPoint.Type == ExtremePointType.VerticalPlateauStartMax &&
                         nextPoint.Type == ExtremePointType.VerticalPlateauEndMin || currentPoint.Type == ExtremePointType.VerticalPlateauStartMin &&
                         nextPoint.Type == ExtremePointType.VerticalPlateauEndMax) && plateauLengthThreshold <=
                        Math.Abs(currentPoint.Point.X - nextPoint.Point.X))
                    {
                        //res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                        //res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                    }
                    //if ((currentPoint.Type == ExtremePointType.HorizontalPlateauStartMax &&
                    //     nextPoint.Type == ExtremePointType.HorizontalPlateauEndMin || currentPoint.Type == ExtremePointType.HorizontalPlateauStartMin &&
                    //     nextPoint.Type == ExtremePointType.HorizontalPlateauEndMax) && plateauLengthThreshold <=
                    //    Math.Abs(currentPoint.Point.Y - nextPoint.Point.Y))
                    //{
                    //    res.Add(new ExtremePoint { Point = currentPoint.Point, Type = ExtremePointType.Curvature });
                    //    res.Add(new ExtremePoint { Point = nextPoint.Point, Type = ExtremePointType.Curvature });
                    //}
                }
            }
            return res;
        }

        private double cityBlock(ExtremePoint one, ExtremePoint two)
        {
            return Math.Abs(one.Sin - two.Sin) +
                   Math.Abs(one.QDir - two.QDir);
        }
        public double WartExtremePoints(List<ExtremePoint> sample, List<ExtremePoint> reference)
        {
            if (reference.First().Type != sample.First().Type)
            {
                reference.RemoveAt(0);
            }

            double finalWeight = double.MaxValue;
            var ewpMatrix = Enumerable.Repeat(-1d, sample.Count * reference.Count).ToArray();
            ewpMatrix[0] = cityBlock(reference[0], sample[0]);
            ewpMatrix[reference.Count*2] = cityBlock(reference[0], sample[2]);
            ewpMatrix[2] = cityBlock(reference[2], sample[0]);
            for (var index = 0; index < ewpMatrix.Length; index++)
            {
                var neigborWeights = new List<double>();
                var i = index % reference.Count;
                var j = index / reference.Count;
                if (i - 1 >= 0 && j - 1 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j-1) + i - 1];
                    if (elem != -1)
                    {
                        neigborWeights.Add(elem + 0.5*cityBlock(reference[i], sample[j]));
                    }
                }

                if (i - 1 >= 0 && j - 3 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j-3) + i-1];
                    if (elem != -1)
                    {
                        var weight = elem + cityBlock(reference[i], sample[j]);
                        if (j - 2 >= 0)
                        {
                            weight += 2 * cityBlock(sample[j - 2], sample[j - 1]);
                        }
                        neigborWeights.Add(weight);
                    }
                }
                if (i - 3 >= 0 && j - 1 >= 0)
                {
                    var elem = ewpMatrix[reference.Count * (j - 1) + i - 3];
                    if (elem != -1)
                    {
                        var weight = elem + cityBlock(reference[i], sample[j]);
                        if (i - 2 >= 0)
                        {
                            weight += 2 * cityBlock(reference[i - 2], reference[i - 1]);
                        }
                        neigborWeights.Add(weight);
                    }
                }
                if (neigborWeights.Any())
                {
                    ewpMatrix[index] = neigborWeights.Min();
                    finalWeight = ewpMatrix[index];
                }
            }

            return finalWeight;
        }

        public List<ExtremePoint> NormalizeValues(List<ExtremePoint> sample)
        {
            var xMax = sample.Max(el => el.Point.X);
            var xMin = sample.Min(el => el.Point.X);
            var yMax = sample.Max(el => el.Point.Y);
            var yMin = sample.Min(el => el.Point.Y);
            //var xMax = sample.Select(list => list.Max(elem => elem.X)).Max();
            //var xMin = sample.Select(list => list.Min(elem => elem.X)).Min();
            //var yMax = sample.Select(list => list.Max(elem => elem.Y)).Max();
            //var yMin = sample.Select(list => list.Min(elem => elem.Y)).Min();
            //var res = sample.Select(el => el.Select(point => new RawPoint
            //    {TimeStamp = point.TimeStamp, X = (point.X - xMin) / xMax, Y = (point.Y - yMin) / yMax}).ToList()).ToList();
            //return res;
            var res = sample.Select(el => new ExtremePoint
            {
                Type = el.Type,
                Point = new RawPoint
                    {TimeStamp = el.Point.TimeStamp, X = (el.Point.X - xMin) / xMax, Y = (el.Point.Y - yMin) / yMax}
            });
            return res.ToList();
        }

        private static double Sin(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dy / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        private static double Cos(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            return dx / Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        private static double QDir(RawPoint current, RawPoint prev)
        {
            const int L = 16;
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var theta = Math.Atan(dy / dx);
            if (theta < (-Math.PI) / 2 + Math.PI / L || theta >= 3 * Math.PI / 2 - Math.PI / L)
            {
                return 1;
            }

            for (var i = 1; i < L; i++)
            {
                if (-Math.PI / 2 + (2 * i - 3) * Math.PI / L <= theta &&
                    theta < -Math.PI / 2 + (2 * i - 1) * Math.PI / L)
                    return i;
            }

            return 0;
        }

        private static double Speed(RawPoint current, RawPoint prev)
        {
            var dy = current.Y - prev.Y;
            var dx = current.X - prev.X;
            var dt = current.TimeStamp - prev.TimeStamp;
            return Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2)) / dt;
        }
    }
}