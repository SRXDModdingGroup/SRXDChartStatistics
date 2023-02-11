using System;
using System.Collections.Generic;
using Util;

namespace ChartMetrics; 

public class MetricResult {
    public static MetricResult Empty { get; } = new(new List<MetricPoint>());
    
    public IReadOnlyList<MetricPoint> Points => points;

    private List<MetricPoint> points;

    public MetricResult(IReadOnlyList<MetricPoint> points) => this.points = new List<MetricPoint>(points);

    public double GetValue(double time) => GetValueBinary(time, 0, points.Count - 1);

    public double SampleRange(double startTime, double endTime) => GetValue(endTime) - GetValue(startTime);

    public List<MetricValue> GetValues(double startTime, double endTime, double resolution) {
        int timeIndex = 0;
        int pointIndex = 1;
        double previousValue = points[0].Value;
        var values = new List<MetricValue>();

        while (true) {
            double time = startTime + timeIndex / resolution;
            double value = GetValueLinear(time, ref pointIndex);

            values.Add(new MetricValue(time, value - previousValue));

            if (time > endTime)
                return values;

            previousValue = value;
            timeIndex++;
        }
    }

    public List<MetricSegment> GetSegments(double minDuration) {
        List<MetricPoint> simplifiedPoints;

        if (minDuration == 0d)
            simplifiedPoints = points;
        else {
            simplifiedPoints = new List<MetricPoint>();
            
            simplifiedPoints.Add(points[0]);
            AddSimplifiedPoint(0, points.Count - 1);
            simplifiedPoints.Add(points[points.Count - 1]);
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
            var start = points[startIndex];
            var end = points[endIndex];
            double slope = (end.Value - start.Value) / (end.Time - start.Time);
            double minDiff = double.MaxValue;
            int bestIndex = -1;

            for (int i = startIndex + 1; i < endIndex; i++) {
                var mid = points[i];
                
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
            simplifiedPoints.Add(points[bestIndex]);
            AddSimplifiedPoint(bestIndex, endIndex);
        }
    }

    private double GetValueBinary(double time, int startIndex, int endIndex) {
        while (startIndex < endIndex) {
            int midIndex = (startIndex + endIndex) / 2;
            double midTime = points[midIndex].Time;

            if (midTime == time)
                return points[midIndex].Value;

            if (time > midTime)
                startIndex = midIndex + 1;
            else
                endIndex = midIndex - 1;
        }

        if (startIndex == points.Count - 1)
            return points[startIndex].Value;

        var first = points[startIndex];
        var second = points[startIndex + 1];

        if (second.Interpolate)
            return MathU.Remap(time, first.Time, second.Time, first.Value, second.Value);

        return first.Value;
    }
    
    private double GetValueLinear(double time, ref int startIndex) {
        while (startIndex < points.Count) {
            double pointTime = points[startIndex].Time;
            
            if (pointTime == time)
                return points[startIndex].Value;

            if (pointTime > time)
                break;
            
            startIndex++;
        }

        if (startIndex == points.Count)
            return points[points.Count - 1].Value;

        if (startIndex == 0)
            return points[0].Value;

        var first = points[startIndex - 1];
        var second = points[startIndex];

        if (second.Interpolate)
            return MathU.Remap(time, first.Time, second.Time, first.Value, second.Value);

        return first.Value;
    }

    public static List<MetricValue> SmoothValues(IList<MetricValue> values, int width) {
        double kernelSum = 0d;

        for (int i = -width; i <= width; i++)
            kernelSum += Kernel((double) i / (width + 1));

        var newValues = new List<MetricValue>(values.Count);

        for (int i = 0; i < values.Count; i++) {
            double sum = 0d;

            for (int j = -width; j <= width; j++) {
                int index = i + j;

                if (index >= 0 && index < values.Count)
                    sum += Kernel((double) j / (width + 1)) * values[index].Value;
            }
            
            newValues.Add(new MetricValue(values[i].Time, sum / kernelSum));
        }

        return newValues;

        double Kernel(double val) {
            val = 1d - Math.Abs(val);

            return val * val * (3d - 2d * val);
        }
    }
}