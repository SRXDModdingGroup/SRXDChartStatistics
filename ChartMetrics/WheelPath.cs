using System;
using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public class WheelPath {
    public static WheelPath Empty { get; } = new(new List<WheelPathPoint>());
    
    private const int HOLD_RESOLUTION = 30;
    private const int SIMPLIFY_ITERATIONS = 16;
    private const float SIMPLIFY_APPROACH_RATE = 0.5f;
    private const double PREFERRED_STOP_BEFORE_TAP_TIME = 0.1d;

    public IReadOnlyList<WheelPathPoint> Points => points;

    private List<WheelPathPoint> points;

    private WheelPath() => points = new List<WheelPathPoint>();

    private WheelPath(List<WheelPathPoint> points) => this.points = points;

    public static WheelPath GenerateFromNotes(IReadOnlyList<Note> notes) {
        var path = new WheelPath();
        var points = path.points;
        bool holding = false;
        bool newPath = true;
        var targetType = TargetType.None;
        double stackTime = notes[0].Time;
        float lanePosition = notes[0].Column;
        float netPosition = 0f;
        float targetLanePosition = 0f;
        var currentColor = notes[0].Color;
        var targetColor = currentColor;
        var matchesInStack = new List<Note>();
        Note holdNote = null;
        Note previousHoldNote = null;

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
            
            UpdateTarget(note);

            if (i < notes.Count - 1 && MathU.AlmostEquals(notes[i + 1].Time, stackTime))
                continue;
            
            if (targetType == TargetType.Match)
                GetTargetFromMatches();

            switch (targetType) {
                case TargetType.Spin:
                    newPath = true;
                    break;
                case TargetType.HoldPoint:
                case TargetType.HoldEnd:
                    GenerateHoldPoints();
                    break;
                default:
                    GeneratePoint();
                    break;
            }

            switch (targetType) {
                case TargetType.Hold:
                    if (note.EndIndex >= 0)
                        holding = true;
                    
                    break;
                case TargetType.Spin:
                case TargetType.HoldEnd:
                    holding = false;
                    break;
            }

            targetType = TargetType.None;
            stackTime = note.Time;
            matchesInStack.Clear();
            holdNote = null;
        }

        return path;

        void UpdateTarget(Note note) {
            var type = note.Type;

            if (targetType == TargetType.Spin) { }
            else if (type == NoteType.SpinRight || type == NoteType.SpinLeft || type == NoteType.Scratch)
                targetType = TargetType.Spin;
            else if (targetType == TargetType.Hold) { }
            else if (type == NoteType.Hold) {
                targetLanePosition = note.Column;
                targetColor = note.Color;
                targetType = TargetType.Hold;
                holdNote = note;
            }
            else if (holding) {
                if (type == NoteType.HoldEnd || type == NoteType.Liftoff) {
                    targetLanePosition = note.Column;
                    targetColor = currentColor;
                    targetType = TargetType.HoldEnd;
                    holdNote = note;
                }
                else if (targetType == TargetType.HoldEnd) { }
                else if (type == NoteType.HoldPoint) {
                    targetLanePosition = note.Column;
                    targetColor = currentColor;
                    targetType = TargetType.HoldEnd;
                    holdNote = note;
                }
            }
            else if (targetType == TargetType.Tap) { }
            else if (type == NoteType.Tap) {
                targetLanePosition = note.Column;
                targetColor = note.Color;
                targetType = TargetType.Tap;
            }
            else if (note.Type == NoteType.Match) {
                matchesInStack.Add(note);
                targetType = TargetType.Match;
            }
        }

        void GetTargetFromMatches() {
            bool anyInCurrentColor = false;
            int columnSum = 0;

            foreach (var note in matchesInStack) {
                if (note.Color == currentColor) {
                    columnSum += note.Column;
                    anyInCurrentColor = true;
                }
                else
                    columnSum += note.Column - 4 * Math.Sign(note.Column);
            }

            targetLanePosition = (float) columnSum / matchesInStack.Count;

            if (anyInCurrentColor)
                targetColor = currentColor;
            else {
                if (currentColor == NoteColor.Blue)
                    targetColor = NoteColor.Red;
                else
                    targetColor = NoteColor.Blue;

                targetLanePosition -= 4f * Math.Sign(targetLanePosition);
            }

            targetType = TargetType.Match;
        }

        void GenerateHoldPoints() {
            if (!holding || previousHoldNote == null)
                return;
            
            double startTime = previousHoldNote.Time;
            float startPosition = previousHoldNote.Column;
            double timeDifference = stackTime - startTime;
            int pointCount = (int) (HOLD_RESOLUTION * timeDifference);
            double pointInterval = timeDifference / pointCount;

            for (int j = 1; j < pointCount; j++) {
                double pointTime = startTime + j * pointInterval;
                float pointPosition = InterpolateHold(startTime, stackTime, startPosition, targetLanePosition, previousHoldNote.CurveType, pointTime);

                points.Add(new WheelPathPoint(pointTime, pointPosition, netPosition + pointPosition - startPosition, currentColor, false));
            }

            netPosition += targetLanePosition - lanePosition;
            lanePosition = targetLanePosition;
            previousHoldNote = holdNote;
            points.Add(new WheelPathPoint(stackTime, lanePosition, netPosition, currentColor, false));
            newPath = false;
        }

        void GeneratePoint() {
            float laneDifference = targetLanePosition - lanePosition;
            float oldNetPosition = netPosition;

            if (targetColor == currentColor)
                netPosition += laneDifference;
            else if (MathU.AlmostEquals(laneDifference, 0f)) {
                if (netPosition > 4.5f)
                    netPosition -= 4f;
                else if (netPosition < -4.5f)
                    netPosition += 4f;
                else
                    netPosition += 4f * Math.Sign(targetLanePosition);
            }
            else if (laneDifference > 0f)
                netPosition += laneDifference - 4f;
            else
                netPosition += laneDifference + 4f;

            if ((targetType == TargetType.Hold || targetType == TargetType.Tap) && !newPath && points.Count > 0) {
                double time = Math.Max(stackTime - PREFERRED_STOP_BEFORE_TAP_TIME, 0.5f * (points[points.Count - 1].Time + stackTime));

                if (targetColor == currentColor)
                    points.Add(new WheelPathPoint(time, targetLanePosition, netPosition, currentColor, false));
                else {
                    lanePosition += netPosition - oldNetPosition;

                    if (lanePosition >= 4f) {
                        lanePosition -= 4f;
                        currentColor = targetColor;
                    }
                    else if (lanePosition <= -4f) {
                        lanePosition += 4f;
                        currentColor = targetColor;
                    }

                    points.Add(new WheelPathPoint(time, lanePosition, netPosition, currentColor, false));
                }

                newPath = true;
            }

            lanePosition = targetLanePosition;
            currentColor = targetColor;
            previousHoldNote = null;
            points.Add(new WheelPathPoint(stackTime, lanePosition, netPosition, targetColor, newPath));
            newPath = false;
        }
    }

    public static WheelPath Simplify(WheelPath path, int iterations = 0) {
        if (iterations < 1)
            iterations = SIMPLIFY_ITERATIONS;

        var points = path.points;
        var newPositions = new List<float>(points.Count);

        foreach (var point in points)
            newPositions.Add(point.NetPosition);

        for (int i = 0; i < iterations; i++) {
            for (int j = 1; j < points.Count - 1; j++) {
                var point = points[j];
                var previous = points[j - 1];
                var next = points[j + 1];
                
                if (point.FirstInPath || next.FirstInPath || point.CurrentColor != previous.CurrentColor || point.CurrentColor != next.CurrentColor)
                    continue;
                
                float maxDeviation = 1f / (10f * Math.Abs((float) (previous.Time - next.Time)) + 1f);
                float targetPosition = MathU.Lerp(newPositions[j], (float) MathU.Remap(point.Time, previous.Time, next.Time, newPositions[j - 1], newPositions[j + 1]), SIMPLIFY_APPROACH_RATE * maxDeviation);
                
                newPositions[j] = MathU.Clamp(targetPosition, point.NetPosition - maxDeviation, point.NetPosition + maxDeviation);
            }
        }

        var newPoints = new List<WheelPathPoint>(newPositions.Count);

        for (int i = 0; i < points.Count; i++) {
            var point = points[i];
            float netPosition = newPositions[i];
            
            newPoints.Add(new WheelPathPoint(point.Time, point.LanePosition + netPosition - point.NetPosition, netPosition, point.CurrentColor, point.FirstInPath));
        }

        return new WheelPath(newPoints);
    }

    private static float InterpolateHold(double startTime, double endTime, float startPosition, float endPosition, CurveType curveType, double pointTime) {
        float interpValue;

        if (curveType == CurveType.Angular) {
            if (startTime < endTime - 0.1d)
                startTime = endTime - 0.1d;
            
            if (pointTime <= startTime)
                return startPosition;
            
            interpValue = (float) (pointTime - startTime) / (float) (endTime - startTime);
        }
        else {
            float interpTime = (float) (pointTime - startTime) / (float) (endTime - startTime);
            
            interpValue = curveType switch {
                CurveType.Cosine => 0.5f * (1f - (float) Math.Cos(Math.PI * interpTime)),
                CurveType.CurveOut => interpTime * interpTime,
                CurveType.CurveIn => 1f - (1f - interpTime) * (1f - interpTime),
                CurveType.Linear => interpTime,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return MathU.Lerp(startPosition, endPosition, interpValue);
    }

    private enum TargetType {
        Spin,
        Hold,
        HoldEnd,
        HoldPoint,
        Tap,
        Match,
        None
    }
}