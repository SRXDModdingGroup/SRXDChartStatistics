using System.Collections.Generic;

namespace ChartMetrics; 

public class ChartRatingData {
    public IReadOnlyDictionary<string, double> Values => values;

    private SortedDictionary<string, double> values;

    public ChartRatingData(SortedDictionary<string, double> values) => this.values = values;

    public double Rate() {
        return 0d;
    }

    public static ChartRatingData Create(ChartData chartData) {
        var result = new PointValue().Calculate(chartData);
        var notes = chartData.Notes;
        double startTime;
        double endTime;

        if (notes.Count == 0) {
            startTime = 0d;
            endTime = 0d;
        }
        else {
            startTime = notes[0].Time;
            endTime = notes[notes.Count - 1].Time;
        }
        
        var weights = result.GetPlot(startTime, endTime, 100d).Smooth(100).Points;
        double weightSum = 0d;

        foreach (var point in weights)
            weightSum += point.Value;

        var ratings = new SortedDictionary<string, double>();

        foreach (var metric in Metric.GetAllMetrics()) {
            if (metric is PointValue)
                continue;
                
            result = metric.Calculate(chartData);

            var points = result.GetPlot(startTime, endTime, 100d).Points;
            double sum = 0d;

            for (int i = 0; i < points.Count && i < weights.Count; i++)
                sum += weights[i].Value * points[i].Value;
                
            ratings.Add(metric.Name, sum * 100d / weightSum);
        }

        return new ChartRatingData(ratings);
    }
}