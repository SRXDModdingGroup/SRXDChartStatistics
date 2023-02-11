using ChartHelper.Types;

namespace ChartMetrics {
    public class SpinDensity : DensityMetric {
        public override string Description => "The density of spins and scratches across the chart";

        protected override bool CountFilter(Note note) => note.Type == NoteType.SpinLeft
                                                          || note.Type == NoteType.SpinRight
                                                          || note.Type == NoteType.Scratch;
    }
}