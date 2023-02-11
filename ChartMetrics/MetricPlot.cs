using System;
using System.Collections.Generic;

namespace ChartMetrics; 

public class MetricPlot {
    public IReadOnlyList<MetricPlotPoint> Points => points;

    private List<MetricPlotPoint> points;

    public MetricPlot(List<MetricPlotPoint> points) => this.points = points;

    public MetricPlot Smooth(int width) {
        double kernelSum = 0d;

        for (int i = -width; i <= width; i++)
            kernelSum += Kernel((double) i / (width + 1));

        var newPoints = new List<MetricPlotPoint>(points.Count);

        for (int i = 0; i < points.Count; i++) {
            double sum = 0d;

            for (int j = -width; j <= width; j++) {
                int index = i + j;

                if (index >= 0 && index < points.Count)
                    sum += Kernel((double) j / (width + 1)) * points[index].Value;
            }
            
            newPoints.Add(new MetricPlotPoint(points[i].Time, sum / kernelSum));
        }

        return new MetricPlot(newPoints);

        double Kernel(double val) {
            val = 1d - Math.Abs(val);

            return val * val * (3d - 2d * val);
        }
    }
}