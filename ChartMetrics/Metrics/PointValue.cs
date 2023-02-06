using System;
using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public class PointValue : Metric {
    private const double MATCH_VALUE = 4d;
    private const double TAP_BEAT_VALUE = 16d;
    private const double SPIN_SCRATCH_VALUE = 12d;
    private const double SUSTAIN_TICK_LENGTH = 1d / 20d;
    
    public override string Description => "The total point value over the course of the chart";
    
    public override MetricResult Calculate(ChartData chartData) {
        var notes = chartData.Notes;
        
        if (notes.Count == 0)
            return MetricResult.Empty;

        var points = new List<MetricPoint>();
        double beatHoldTime = -1d;
        double sustainTime = -1d;
        double sum = 0;
        double stackTime = notes[0].Time;

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
            var type = note.Type;
            double oldSum = sum;
            double time = note.Time;

            switch (type) {
                case NoteType.Match:
                    sum += MATCH_VALUE;
                    break;
                case NoteType.Beat:
                case NoteType.Tap:
                case NoteType.Hold:
                case NoteType.Liftoff:
                case NoteType.BeatReleaseHard:
                    sum += TAP_BEAT_VALUE;
                    break;
                case NoteType.SpinRight:
                case NoteType.SpinLeft:
                case NoteType.Scratch:
                    sum += SPIN_SCRATCH_VALUE;
                    break;
            }

            switch (type) {
                case NoteType.Match:
                case NoteType.Beat:
                case NoteType.SpinRight:
                case NoteType.SpinLeft:
                case NoteType.Hold:
                case NoteType.Tap:
                case NoteType.Scratch:
                case NoteType.HoldEnd:
                case NoteType.Liftoff:
                case NoteType.SpinEnd:
                    UpdateSustains();
                    sustainTime = -1d;
                    break;
                case NoteType.BeatReleaseSoft:
                case NoteType.BeatReleaseHard:
                    UpdateSustains();
                    beatHoldTime = -1d;
                    break;
            }
            
            if (note.EndIndex >= 0) {
                switch (type) {
                    case NoteType.Beat:
                        beatHoldTime = time;
                        break;
                    case NoteType.SpinRight:
                    case NoteType.SpinLeft:
                    case NoteType.Scratch:
                    case NoteType.Hold:
                        sustainTime = time;
                        break;
                }
            }

            if (i < notes.Count - 1 && MathU.AlmostEquals(notes[i + 1].Time, stackTime))
                continue;
            
            UpdateSustains();

            if (sum > oldSum)
                points.Add(new MetricPoint(stackTime, sum));

            if (i < notes.Count - 1)
                stackTime = notes[i + 1].Time;
        }

        return new MetricResult(points);

        void UpdateSustains() {
            if (sustainTime < 0d)
                return;

            while (sustainTime < stackTime || beatHoldTime < stackTime) {
                if (sustainTime < stackTime) {
                    sustainTime += SUSTAIN_TICK_LENGTH;
                    sum++;
                }
                
                if (beatHoldTime < stackTime) {
                    beatHoldTime += SUSTAIN_TICK_LENGTH;
                    sum++;
                }
                
                if (sustainTime > points[points.Count - 1].Time)
                    points.Add(new MetricPoint(sustainTime, sum));
            }
        }
    }
}