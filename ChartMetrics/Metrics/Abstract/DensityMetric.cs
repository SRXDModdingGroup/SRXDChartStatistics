using System;
using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public abstract class DensityMetric : Metric {
    protected abstract bool CountFilter(Note note);
        
    public override MetricResult Calculate(ChartData chartData) {
        var notes = chartData.Notes;

        if (notes.Count == 0)
            return MetricResult.Empty;
            
        var points = new List<MetricPoint>();
        int count = 0;
        double stackTime = notes[0].Time;
        bool stackIsCandidate = false;

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
                
            if (CountFilter(note)) {
                count++;
                stackIsCandidate = true;
            }

            if (i < notes.Count - 1 && MathU.AlmostEquals(notes[i + 1].Time, stackTime))
                continue;
                
            if (stackIsCandidate)
                points.Add(new MetricPoint(stackTime, count, false));

            if (i < notes.Count - 1)
                stackTime = notes[i + 1].Time;
                
            stackIsCandidate = false;
        }

        return new MetricResult(points);
    }
}