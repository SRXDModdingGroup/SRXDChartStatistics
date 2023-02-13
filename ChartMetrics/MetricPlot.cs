using System;
using System.Collections.Generic;

namespace ChartMetrics; 

public class MetricPlot {
    public IReadOnlyList<double> Points { get; }

    public IReadOnlyList<double> Sorted { get; }
    
    public double StartTime { get; }
    
    public double EndTime { get; }

    private int firstNonZero;

    public MetricPlot(IReadOnlyList<double> points, double startTime, double endTime) {
        Points = points;
        StartTime = startTime;
        EndTime = endTime;
        
        var sorted = new List<double>(points);
        
        sorted.Sort();

        for (int i = 0; i < sorted.Count; i++) {
            if (sorted[i] == 0d)
                continue;

            firstNonZero = i;

            break;
        }

        Sorted = sorted;
    }

    public double GetQuantile(double quantile) {
        int index = firstNonZero + (int) (quantile * (Sorted.Count - firstNonZero));
        
        if (index < firstNonZero)
            return Sorted[firstNonZero];

        if (index >= Sorted.Count)
            return Sorted[Sorted.Count - 1];

        return Sorted[index];
    }

    public MetricPlot Smooth(int width) {
        double kernelSum = 0d;

        for (int i = -width; i <= width; i++)
            kernelSum += Kernel((double) i / (width + 1));

        var newPoints = new List<double>(Points.Count);

        for (int i = 0; i < Points.Count; i++) {
            double sum = 0d;

            for (int j = -width; j <= width; j++) {
                int index = i + j;

                if (index >= 0 && index < Points.Count)
                    sum += Kernel((double) j / (width + 1)) * Points[index];
            }
            
            newPoints.Add(sum / kernelSum);
        }

        return new MetricPlot(newPoints, StartTime, EndTime);

        double Kernel(double val) {
            val = 1d - Math.Abs(val);

            return val * val * (3d - 2d * val);
        }
    }
}