using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChartHelper;
using Util;

namespace ChartMetrics {
    public class TapBeatComplexity : ComplexityMetric {
        public override string Description => "The complexity of the sequence of taps, beats, liftoffs, and hard beat releases";
        
        protected override byte Selector(NoteType type) {
            switch (type) {
                case NoteType.Tap:
                case NoteType.Hold:
                case NoteType.Liftoff:
                    return 1;
                case NoteType.Beat:
                case NoteType.BeatReleaseHard:
                    return 1 << 1;
            }

            return 0;
        }
    }
}