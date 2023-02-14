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

    public IReadOnlyList<WheelPathPoint> Points { get; }

    private WheelPath(IReadOnlyList<WheelPathPoint> points) => Points = points;

    public WheelPath Simplify(int iterations = 0) {
        if (iterations < 1)
            iterations = SIMPLIFY_ITERATIONS;

        var newPositions = new List<float>(Points.Count);

        foreach (var point in Points)
            newPositions.Add(point.NetPosition);

        for (int i = 0; i < iterations; i++) {
            for (int j = 1; j < Points.Count - 1; j++) {
                var point = Points[j];
                var previous = Points[j - 1];
                var next = Points[j + 1];
                
                if (point.FirstInPath || next.FirstInPath || point.CurrentColor != previous.CurrentColor || point.CurrentColor != next.CurrentColor)
                    continue;
                
                float maxDeviation = 1.5f / (10f * Math.Abs((float) (previous.Time - next.Time)) + 1f);
                float targetPosition = MathU.Lerp(newPositions[j], (float) MathU.Remap(point.Time, previous.Time, next.Time, newPositions[j - 1], newPositions[j + 1]), SIMPLIFY_APPROACH_RATE * maxDeviation);
                
                newPositions[j] = MathU.Clamp(targetPosition, point.NetPosition - maxDeviation, point.NetPosition + maxDeviation);
            }
        }

        var newPoints = new List<WheelPathPoint>(newPositions.Count);

        for (int i = 0; i < Points.Count; i++) {
            var point = Points[i];
            float netPosition = newPositions[i];
            
            newPoints.Add(new WheelPathPoint(point.Time, point.LanePosition + netPosition - point.NetPosition, netPosition, point.CurrentColor, point.FirstInPath));
        }

        return new WheelPath(newPoints);
    }

    public static WheelPath Create(IReadOnlyList<Note> notes) {
        var points = new List<WheelPathPoint>();

        if (notes.Count == 0)
            return new WheelPath(points);

        bool wasSpinning = true;
        float lanePosition = notes[0].Column;
        float netPosition = 0f;
        var currentColor = notes[0].Color;
        var matchesInStack = new List<Note>();
        int skipMatchesUntil = 0;

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
            double time = note.Time;
            var type = note.Type;

            switch (type) {
                case NoteType.Match:
                    for (int j = Math.Max(i, skipMatchesUntil); j < notes.Count; j++) {
                        var other = notes[j];

                        if (!MathU.AlmostEquals(other.Time, time))
                            break;
                        
                        if (other.Type == NoteType.Match)
                            matchesInStack.Add(other);

                        skipMatchesUntil = j + 1;
                    }

                    if (matchesInStack.Count == 0)
                        break;
                    
                    GetTargetFromMatches(matchesInStack, currentColor, out float targetLanePosition, out var targetColor);
                    GeneratePoint(time, targetLanePosition, targetColor, false);
                    matchesInStack.Clear();
                    
                    break;
                case NoteType.SpinRight:
                case NoteType.SpinLeft:
                case NoteType.Scratch:
                    wasSpinning = true;
                    break;
                case NoteType.Hold:
                    GeneratePoint(time, note.Column, note.Color, true);

                    int endIndex = note.EndIndex;
                    
                    if (endIndex < 0)
                        break;

                    for (; i <= endIndex; i++) {
                        var other = notes[i];
                        var otherType = other.Type;

                        if (otherType != NoteType.HoldPoint && otherType != NoteType.HoldEnd && otherType != NoteType.Liftoff)
                            continue;
                        
                        GenerateHoldPoints(note, other.Time, other.Column);
                        GeneratePoint(other.Time, other.Column, currentColor, false);
                        note = other;
                    }

                    i = endIndex;
                    
                    break;
                case NoteType.Tap:
                    GeneratePoint(time, note.Column, note.Color, true);
                    break;
            }
        }

        return new WheelPath(points);

        void GenerateHoldPoints(Note holdNote, double endTime, float endPosition) {
            double startTime = holdNote.Time;
            
            if (MathU.AlmostEquals(startTime, endTime))
                return;
            
            float startPosition = holdNote.Column;
            double timeDifference = endTime - startTime;
            int pointCount = (int) (HOLD_RESOLUTION * timeDifference);
            double pointInterval = timeDifference / pointCount;

            for (int j = 1; j < pointCount; j++) {
                double pointTime = startTime + j * pointInterval;
                float pointPosition = InterpolateHold(startTime, endTime, startPosition, endPosition, holdNote.CurveType, pointTime);

                points.Add(new WheelPathPoint(pointTime, pointPosition, netPosition + pointPosition - startPosition, currentColor, false));
            }
        }

        void GeneratePoint(double time, float targetLanePosition, NoteColor targetColor, bool newPath) {
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

            if (newPath && !wasSpinning && points.Count > 0) {
                double stopTime = Math.Max(time - PREFERRED_STOP_BEFORE_TAP_TIME, 0.5f * (points[points.Count - 1].Time + time));

                if (stopTime <= points[points.Count - 1].Time) { }
                else if (targetColor == currentColor)
                    points.Add(new WheelPathPoint(stopTime, targetLanePosition, netPosition, currentColor, false));
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

                    points.Add(new WheelPathPoint(stopTime, lanePosition, netPosition, currentColor, false));
                }
            }

            lanePosition = targetLanePosition;
            currentColor = targetColor;
            
            if (points.Count == 0 || time > points[points.Count - 1].Time)
                points.Add(new WheelPathPoint(time, lanePosition, netPosition, targetColor, newPath || wasSpinning));
            
            wasSpinning = false;
        }
    }
    
    private static void GetTargetFromMatches(List<Note> matches, NoteColor currentColor, out float targetLanePosition, out NoteColor targetColor) {
        bool anyInCurrentColor = false;
        int columnSum = 0;

        foreach (var note in matches) {
            if (note.Color == currentColor) {
                columnSum += note.Column;
                anyInCurrentColor = true;
            }
            else
                columnSum += note.Column - 4 * Math.Sign(note.Column);
        }

        targetLanePosition = (float) columnSum / matches.Count;

        if (anyInCurrentColor)
            targetColor = currentColor;
        else {
            if (currentColor == NoteColor.Blue)
                targetColor = NoteColor.Red;
            else
                targetColor = NoteColor.Blue;

            targetLanePosition -= 4f * Math.Sign(targetLanePosition);
        }
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
                CurveType.Linear or _ => interpTime
            };
        }

        return MathU.Lerp(startPosition, endPosition, interpValue);
    }
}