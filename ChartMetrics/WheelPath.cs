using System;
using System.Collections.Generic;
using ChartHelper.Types;
using Util;

namespace ChartMetrics; 

public class WheelPath {
    public static WheelPath Empty { get; } = new(new List<WheelPathPoint>());
    
    private const int HOLD_RESOLUTION = 30;
    private const int SIMPLIFY_ITERATIONS = 16;
    private const double SIMPLIFY_APPROACH_RATIO = 0.5d;
    private const double MIN_SIMPLIFY_APPROACH_AMOUNT = 0.1d;
    private const double PREFERRED_STOP_BEFORE_TAP_TIME = 0.1d;
    private const double AVERAGE_WIDTH = 0.05d;

    public IReadOnlyList<WheelPathPoint> Points { get; }

    private WheelPath(IReadOnlyList<WheelPathPoint> points) => Points = points;

    public WheelPath Simplify(int iterations = 0) {
        if (iterations < 1)
            iterations = SIMPLIFY_ITERATIONS;

        double[] newPositions = new double[Points.Count];
        double[] maxDeviations = new double[Points.Count];

        for (int i = 0; i < Points.Count; i++) {
            var point = Points[i];
            double position = point.NetPosition;
            
            newPositions[i] = position;
            maxDeviations[i] = Math.Min(1.1d * Math.Abs(point.NetPosition - GetAverage(Points, i)), point.IsHoldPoint ? 4d : 1.5d);
        }

        for (int i = 0; i < iterations; i++) {
            for (int j = 1; j < Points.Count; j++) {
                var point = Points[j];
                
                if (point.FirstInPath
                    || point.CurrentColor != Points[j - 1].CurrentColor
                    || j < Points.Count - 1 && point.CurrentColor != Points[j + 1].CurrentColor)
                    continue;

                double maxDeviation = maxDeviations[j];
                double targetPosition;

                if (j == Points.Count - 1 || Points[j + 1].FirstInPath) {
                    if (!point.IsHoldPoint)
                        continue;
                    
                    targetPosition = newPositions[j - 1];
                }
                else
                    targetPosition = MathU.Remap(point.Time, Points[j - 1].Time, Points[j + 1].Time, newPositions[j - 1], newPositions[j + 1]);

                double position = newPositions[j];
                double delta = targetPosition - position;
                double absDelta = Math.Abs(delta);

                if (absDelta <= MIN_SIMPLIFY_APPROACH_AMOUNT)
                    position = targetPosition;
                else if (SIMPLIFY_APPROACH_RATIO * absDelta < MIN_SIMPLIFY_APPROACH_AMOUNT)
                    position += MIN_SIMPLIFY_APPROACH_AMOUNT * Math.Sign(delta);
                else
                    position += SIMPLIFY_APPROACH_RATIO * delta;

                newPositions[j] = MathU.Clamp(position, point.NetPosition - maxDeviation, point.NetPosition + maxDeviation);
            }
        }

        var newPoints = new List<WheelPathPoint>(newPositions.Length);

        for (int i = 0; i < Points.Count; i++) {
            var point = Points[i];
            double netPosition = newPositions[i];
            
            newPoints.Add(new WheelPathPoint(point.Time, point.LanePosition + netPosition - point.NetPosition, netPosition, point.CurrentColor, point.FirstInPath, point.IsHoldPoint));
        }

        return new WheelPath(newPoints);
    }

    public static WheelPath Create(IReadOnlyList<Note> notes) {
        var points = new List<WheelPathPoint>();

        if (notes.Count == 0)
            return new WheelPath(points);

        bool wasSpinning = true;
        double lanePosition = notes[0].Column;
        double netPosition = 0d;
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
                    
                    GetTargetFromMatches(matchesInStack, currentColor, out double targetLanePosition, out var targetColor);
                    GeneratePoint(time, targetLanePosition, targetColor, false, false);
                    matchesInStack.Clear();
                    
                    break;
                case NoteType.SpinRight:
                case NoteType.SpinLeft:
                case NoteType.Scratch:
                    wasSpinning = true;
                    netPosition = 0d;
                    lanePosition = 0d;
                    break;
                case NoteType.Hold:
                    GeneratePoint(time, note.Column, note.Color, true, false);

                    int endIndex = note.EndIndex;
                    
                    if (endIndex < 0)
                        break;

                    for (; i <= endIndex; i++) {
                        var other = notes[i];
                        var otherType = other.Type;

                        if (otherType != NoteType.HoldPoint && otherType != NoteType.HoldEnd && otherType != NoteType.Liftoff)
                            continue;
                        
                        GenerateHoldPoints(note, other.Time, other.Column);
                        GeneratePoint(other.Time, other.Column, currentColor, false, true);
                        note = other;
                    }

                    i = endIndex;
                    
                    break;
                case NoteType.Tap:
                    GeneratePoint(time, note.Column, note.Color, true, false);
                    break;
            }
        }

        return new WheelPath(points);

        void GenerateHoldPoints(Note holdNote, double endTime, double endPosition) {
            double startTime = holdNote.Time;
            
            if (MathU.AlmostEquals(startTime, endTime))
                return;
            
            double startPosition = holdNote.Column;
            double timeDifference = endTime - startTime;
            int pointCount = (int) (HOLD_RESOLUTION * timeDifference);
            double pointInterval = timeDifference / pointCount;

            for (int j = 1; j < pointCount; j++) {
                double pointTime = startTime + j * pointInterval;
                double pointPosition = InterpolateHold(startTime, endTime, startPosition, endPosition, holdNote.CurveType, pointTime);

                points.Add(new WheelPathPoint(pointTime, pointPosition, netPosition + pointPosition - startPosition, currentColor, false, true));
            }
        }

        void GeneratePoint(double time, double targetLanePosition, NoteColor targetColor, bool newPath, bool isHoldPoint) {
            if (wasSpinning) {
                lanePosition = targetLanePosition;
                currentColor = targetColor;
                points.Add(new WheelPathPoint(time, lanePosition, netPosition, targetColor, true, isHoldPoint));
                wasSpinning = false;

                return;
            }

            double laneDifference = targetLanePosition - lanePosition;
            double oldNetPosition = netPosition;

            if (targetColor == currentColor)
                netPosition += laneDifference;
            else if (MathU.AlmostEquals(laneDifference, 0d)) {
                if (netPosition > 4.5d)
                    netPosition -= 4d;
                else if (netPosition < -4.5d)
                    netPosition += 4d;
                else
                    netPosition += 4d * Math.Sign(targetLanePosition);
            }
            else if (laneDifference > 0d)
                netPosition += laneDifference - 4d;
            else
                netPosition += laneDifference + 4d;

            if (newPath && points.Count > 0) {
                double stopTime = Math.Max(time - PREFERRED_STOP_BEFORE_TAP_TIME, 0.5d * (points[points.Count - 1].Time + time));

                if (stopTime <= points[points.Count - 1].Time) { }
                else if (targetColor == currentColor)
                    points.Add(new WheelPathPoint(stopTime, targetLanePosition, netPosition, currentColor, false, false));
                else {
                    lanePosition += netPosition - oldNetPosition;

                    if (lanePosition >= 4d) {
                        lanePosition -= 4d;
                        currentColor = targetColor;
                    }
                    else if (lanePosition <= -4d) {
                        lanePosition += 4d;
                        currentColor = targetColor;
                    }

                    points.Add(new WheelPathPoint(stopTime, lanePosition, netPosition, currentColor, false, false));
                }
            }

            lanePosition = targetLanePosition;
            currentColor = targetColor;
            
            if (points.Count == 0 || time > points[points.Count - 1].Time)
                points.Add(new WheelPathPoint(time, lanePosition, netPosition, targetColor, newPath, isHoldPoint));
        }
    }
    
    private static void GetTargetFromMatches(List<Note> matches, NoteColor currentColor, out double targetLanePosition, out NoteColor targetColor) {
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

        targetLanePosition = (double) columnSum / matches.Count;

        if (anyInCurrentColor)
            targetColor = currentColor;
        else {
            if (currentColor == NoteColor.Blue)
                targetColor = NoteColor.Red;
            else
                targetColor = NoteColor.Blue;

            targetLanePosition -= 4d * Math.Sign(targetLanePosition);
        }
    }
    
    private static double GetAverage(IReadOnlyList<WheelPathPoint> points, int mid) {
        double midTime = points[mid].Time;
        double startTime = midTime - AVERAGE_WIDTH;
        double endTime = midTime + AVERAGE_WIDTH;
        int startIndex = 0;

        for (int i = mid; i > 0; i--) {
            var point = points[i];

            if (point.FirstInPath || point.Time <= startTime) {
                startIndex = i;
                
                break;
            }
        }

        double sum = 0d;

        for (int i = startIndex; i < points.Count; i++) {
            var point = points[i];

            if (point.Time >= endTime)
                break;

            if ((i == 0 || point.FirstInPath) && point.Time > startTime)
                sum += (point.Time - startTime) * point.NetPosition;
            
            if ((i == points.Count - 1 || points[i + 1].FirstInPath) && point.Time < endTime) {
                sum += (endTime - point.Time) * point.NetPosition;
                
                break;
            }

            var next = points[i + 1];
            double firstTime;
            double firstValue;

            if (point.Time < startTime) {
                firstTime = startTime;
                firstValue = MathU.Remap(startTime, point.Time, next.Time, point.NetPosition, next.NetPosition);
            }
            else {
                firstTime = point.Time;
                firstValue = point.NetPosition;
            }

            double secondTime;
            double secondValue;

            if (next.Time > endTime) {
                secondTime = endTime;
                secondValue = MathU.Remap(endTime, point.Time, next.Time, point.NetPosition, next.NetPosition);
            }
            else {
                secondTime = next.Time;
                secondValue = next.NetPosition;
            }

            sum += IntegrateSegment(firstTime, secondTime, firstValue, secondValue);
        }

        return sum / (2d * AVERAGE_WIDTH);

        double IntegrateSegment(double startTime, double endTime, double startValue, double endValue) => 0.5d * (endTime - startTime) * (startValue + endValue);
    }

    private static double InterpolateHold(double startTime, double endTime, double startPosition, double endPosition, CurveType curveType, double pointTime) {
        double interpValue;

        if (curveType == CurveType.Angular) {
            if (startTime < endTime - 0.1d)
                startTime = endTime - 0.1d;
            
            if (pointTime <= startTime)
                return startPosition;
            
            interpValue = (pointTime - startTime) / (endTime - startTime);
        }
        else {
            double interpTime = (pointTime - startTime) / (endTime - startTime);
            
            interpValue = curveType switch {
                CurveType.Cosine => 0.5d * (1d - Math.Cos(Math.PI * interpTime)),
                CurveType.CurveOut => interpTime * interpTime,
                CurveType.CurveIn => 1d - (1d - interpTime) * (1d - interpTime),
                CurveType.Linear or _ => interpTime
            };
        }

        return MathU.Lerp(startPosition, endPosition, interpValue);
    }
}