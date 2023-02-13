using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public class PointValue : Metric {
    private const int MATCH_VALUE = 4;
    private const int TAP_BEAT_VALUE = 16;
    private const int SPIN_SCRATCH_VALUE = 12;
    private const double SUSTAIN_TICK_RATE = 20d;
    private const double FRAME_RATE = 60d;
    private const double MIN_FRAME_DISTANCE_TO_POINT = 1d / FRAME_RATE;
    
    public override string Description => "The total point value over the course of the chart";
    
    public override MetricResult Calculate(ChartData chartData) {
        var notes = chartData.Notes;
        
        if (notes.Count == 0)
            return MetricResult.Empty;

        var points = new List<MetricPoint>();
        double stackTime = notes[0].Time;
        double beatHoldStartTime = double.MaxValue;
        double beatHoldEndTime = double.MaxValue;
        double sustainStartTime = double.MaxValue;
        double sustainEndTime = double.MaxValue;
        int beatHoldValue = 0;
        int sustainValue = 0;
        int pointsToAdd = 0;
        int sustainIndex = -1;
        int beatHoldIndex = -1;
        long sum = 0L;
        long frame = 0L;
        
        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
            var type = note.Type;

            switch (type) {
                case NoteType.Match:
                    pointsToAdd += MATCH_VALUE;
                    break;
                case NoteType.Beat:
                case NoteType.Tap:
                case NoteType.Hold:
                case NoteType.Liftoff:
                case NoteType.BeatReleaseHard:
                    pointsToAdd += TAP_BEAT_VALUE;
                    break;
                case NoteType.SpinRight:
                case NoteType.SpinLeft:
                case NoteType.Scratch:
                    pointsToAdd += SPIN_SCRATCH_VALUE;
                    break;
            }
            
            if (note.EndIndex >= 0) {
                switch (type) {
                    case NoteType.Beat:
                        beatHoldIndex = i;
                        break;
                    case NoteType.SpinRight:
                    case NoteType.SpinLeft:
                    case NoteType.Scratch:
                    case NoteType.Hold:
                        sustainIndex = i;
                        break;
                }
            }

            if (i < notes.Count - 1 && MathU.AlmostEquals(notes[i + 1].Time, stackTime))
                continue;

            for (; ; frame++) {
                double frameTime = frame / FRAME_RATE;
                
                if (frameTime >= stackTime)
                    break;
                
                int sustainPointsToAdd = 0;

                if (frameTime > sustainStartTime && frameTime < sustainEndTime) {
                    int newSustainValue = (int) (SUSTAIN_TICK_RATE * (frameTime - sustainStartTime));
                    
                    sustainPointsToAdd += newSustainValue - sustainValue;
                    sustainValue = newSustainValue;
                }
                
                if (frameTime > beatHoldStartTime && frameTime < beatHoldEndTime) {
                    int newBeatHoldValue = (int) (SUSTAIN_TICK_RATE * (frameTime - beatHoldStartTime));
                    
                    sustainPointsToAdd += newBeatHoldValue - beatHoldValue;
                    beatHoldValue = newBeatHoldValue;
                }

                if (sustainPointsToAdd == 0)
                    continue;
                
                sum += sustainPointsToAdd;
                
                if (frameTime > stackTime - MIN_FRAME_DISTANCE_TO_POINT || points.Count > 0 && frameTime < points[points.Count - 1].Time + MIN_FRAME_DISTANCE_TO_POINT)
                    continue;
                    
                points.Add(new MetricPoint(frameTime, sum, false));
            }

            if (pointsToAdd > 0 || points.Count > 0 && sum > points[points.Count - 1].Value) {
                sum += pointsToAdd;
                points.Add(new MetricPoint(stackTime, sum, false));
            }

            if (i == notes.Count - 1)
                break;
            
            stackTime = notes[i + 1].Time;
            pointsToAdd = 0;

            if (sustainIndex >= 0) {
                var sustain = notes[sustainIndex];
                
                sustainStartTime = sustain.Time;
                sustainEndTime = notes[sustain.EndIndex].Time;
                sustainValue = 0;
            }

            if (beatHoldIndex >= 0) {
                var beatHold = notes[beatHoldIndex];
                
                beatHoldStartTime = beatHold.Time;
                beatHoldEndTime = notes[beatHold.EndIndex].Time;
                beatHoldValue = 0;
            }

            sustainIndex = -1;
            beatHoldIndex = -1;
        }

        return new MetricResult(points);
    }
}