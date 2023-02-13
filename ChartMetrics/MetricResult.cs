using System;
using System.Collections.Generic;
using Util;

namespace ChartMetrics; 

public class MetricResult {
    public static MetricResult Empty { get; } = new(new List<MetricPoint>());
    
    public IReadOnlyList<MetricPoint> Points { get; }

    public MetricResult(IReadOnlyList<MetricPoint> points) => Points = points;

    public double GetValue(double time) => GetValueBinary(time, 0, Points.Count - 1);

    public double SampleRange(double startTime, double endTime) => GetValue(endTime) - GetValue(startTime);

    public MetricPlot GetPlot(double startTime, double endTime, double resolution) {
        int timeIndex = 0;
        int pointIndex = 1;
        var plotPoints = new List<double>();

        if (Points.Count == 0)
            return new MetricPlot(plotPoints, startTime, endTime);
        
        double previousValue = Points[0].Value;

        while (true) {
            double time = startTime + timeIndex / resolution;
            double value = GetValueLinear(time, ref pointIndex);

            plotPoints.Add(value - previousValue);

            if (time > endTime)
                return new MetricPlot(plotPoints, startTime, time);

            previousValue = value;
            timeIndex++;
        }
    }

    public List<MetricSegment> GetSegments(double minDuration) {
        List<MetricPoint> simplifiedPoints;

        if (minDuration == 0d)
            simplifiedPoints = new List<MetricPoint>(Points);
        else {
            simplifiedPoints = new List<MetricPoint>();
            
            simplifiedPoints.Add(Points[0]);
            AddSimplifiedPoint(0, Points.Count - 1);
            simplifiedPoints.Add(Points[Points.Count - 1]);
        }

        var segments = new List<MetricSegment>();

        for (int i = 0; i < simplifiedPoints.Count - 1; i++) {
            var first = simplifiedPoints[i];
            var second = simplifiedPoints[i + 1];
            double startValue;

            if (first.Interpolate)
                startValue = first.Value;
            else if (i > 0)
                startValue = simplifiedPoints[i - 1].Value;
            else
                startValue = 0d;

            double endValue;

            if (second.Interpolate || i == simplifiedPoints.Count - 2)
                endValue = second.Value;
            else
                endValue = first.Value;
            
            segments.Add(new MetricSegment(first.Time, second.Time, endValue - startValue));
        }

        return segments;

        void AddSimplifiedPoint(int startIndex, int endIndex) {
            var start = Points[startIndex];
            var end = Points[endIndex];
            double slope = (end.Value - start.Value) / (end.Time - start.Time);
            double minDiff = double.MaxValue;
            int bestIndex = -1;

            for (int i = startIndex + 1; i < endIndex; i++) {
                var mid = Points[i];
                
                if (mid.Time - start.Time < minDuration || end.Time - mid.Time < minDuration)
                    continue;
                
                double diff = Math.Abs(mid.Value - (slope * (mid.Time - start.Time) + start.Value));
                
                if (diff >= minDiff)
                    continue;

                minDiff = diff;
                bestIndex = i;
            }
            
            if (bestIndex < 0)
                return;
            
            AddSimplifiedPoint(startIndex, bestIndex);
            simplifiedPoints.Add(Points[bestIndex]);
            AddSimplifiedPoint(bestIndex, endIndex);
        }
    }

    private double GetValueBinary(double time, int startIndex, int endIndex) {
        while (startIndex < endIndex) {
            int midIndex = (startIndex + endIndex) / 2;
            double midTime = Points[midIndex].Time;

            if (midTime == time)
                return Points[midIndex].Value;

            if (time > midTime)
                startIndex = midIndex + 1;
            else
                endIndex = midIndex - 1;
        }

        if (startIndex == Points.Count - 1)
            return Points[startIndex].Value;

        var first = Points[startIndex];
        var second = Points[startIndex + 1];

        if (second.Interpolate)
            return MathU.Remap(time, first.Time, second.Time, first.Value, second.Value);

        return first.Value;
    }
    
    private double GetValueLinear(double time, ref int startIndex) {
        while (startIndex < Points.Count) {
            double pointTime = Points[startIndex].Time;
            
            if (pointTime == time)
                return Points[startIndex].Value;

            if (pointTime > time)
                break;
            
            startIndex++;
        }

        if (startIndex == Points.Count)
            return Points[Points.Count - 1].Value;

        if (startIndex == 0)
            return Points[0].Value;

        var first = Points[startIndex - 1];
        var second = Points[startIndex];

        if (second.Interpolate)
            return MathU.Remap(time, first.Time, second.Time, first.Value, second.Value);

        return first.Value;
    }
}