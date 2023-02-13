using ChartHelper.Types;

namespace ChartMetrics; 

public class OverallNoteDensity : DensityMetric {
    public override string Description => "The density of notes of any type across the chart";

    protected override bool CountFilter(Note note) => true;
}