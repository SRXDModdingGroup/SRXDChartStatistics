using System;
using System.Collections.Generic;
using System.Linq;
using ChartHelper;
using Util;
using Math = System.Math;

namespace ChartMetrics {
    public static class WheelPath {
        private static readonly int HOLD_RESOLUTION = 30;
        private static readonly int SIMPLIFY_ITERATIONS = 16;
        private static readonly float SIMPLIFY_APPROACH_RATE = 0.5f;
        private static readonly float PREFERRED_STOP_BEFORE_TAP_TIME = 0.1f;
        
        public readonly struct Point {
            public float Time { get; }
            
            public float LanePosition { get; }
            
            public float NetPosition { get; }
            
            public NoteColor CurrentColor { get; }

            internal Point(float time, float lanePosition, float netPosition, NoteColor currentColor) {
                Time = time;
                LanePosition = lanePosition;
                NetPosition = netPosition;
                CurrentColor = currentColor;
            }
        }

        internal static List<List<Point>> GeneratePaths(IList<Note> notes, int startIndex, int endIndex) {
            var paths = new List<List<Point>>();
            var points = new List<Point>();
            bool holding = false;
            bool holdNoteFound = false;
            bool holdPointFound = false;
            bool tapNoteFound = false;
            bool holdEndFound = false;
            bool spinFound = false;
            bool targetNoteFound = false;
            float stackTime = notes[startIndex].Time;
            float lanePosition = notes[startIndex].Column;
            float netPosition = 0f;
            float targetLanePosition = 0f;
            var currentColor = notes[startIndex].Color;
            var targetColor = currentColor;
            var matchesInStack = new List<int>();
            Note holdNote = null;
            Note previousHoldNote = null;

            for (int i = startIndex; i <= endIndex; i++) {
                var note = notes[i];
                var type = note.Type;
                
                if (!MathU.AlmostEquals(note.Time, stackTime) || i == endIndex) {
                    if (!targetNoteFound && matchesInStack.Count > 0) {
                        bool anyInCurrentColor = false;
                        int columnSum = 0;
                    
                        foreach (int index in matchesInStack) {
                            var match = notes[index];
                        
                            if (match.Color == currentColor) {
                                columnSum += match.Column;
                                anyInCurrentColor = true;
                            }
                            else
                                columnSum += match.Column - 4 * Math.Sign(match.Column);
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

                        targetNoteFound = true;
                    }
                    
                    if (targetNoteFound) {
                        if (holdPointFound && holding && previousHoldNote != null) {
                            float startTime = previousHoldNote.Time;
                            float startPosition = previousHoldNote.Column;
                            float timeDifference = stackTime - startTime;
                            int pointCount = (int) (HOLD_RESOLUTION * timeDifference);
                            float pointInterval = timeDifference / pointCount;

                            for (int j = 1; j < pointCount; j++) {
                                float timeOffset = j * pointInterval;
                                float pointTime = startTime + timeOffset;
                                float interpTime = timeOffset / timeDifference;
                                float interpValue;
                                
                                switch (previousHoldNote.CurveType) {
                                    case CurveType.Cosine:
                                        interpValue = 0.5f * (1f - (float) Math.Cos(Math.PI * interpTime));
                                        
                                        break;
                                    case CurveType.CurveOut:
                                        interpValue = interpTime * interpTime;
                                        
                                        break;
                                    case CurveType.CurveIn:
                                        interpTime = 1f - interpTime;
                                        interpValue = 1f - interpTime * interpTime;
                                        
                                        break;
                                    case CurveType.Linear:
                                        interpValue = interpTime;
                                        
                                        break;
                                    default:
                                        if (timeDifference < 0.1f) {
                                            interpValue = interpTime;
                                            
                                            break;
                                        }

                                        if (pointTime < stackTime - 0.1f) {
                                            interpValue = 0f;

                                            break;
                                        }

                                        interpValue = 1f - 10f * (stackTime - pointTime);
                                        
                                        break;
                                }

                                float pointPosition = MathU.Lerp(startPosition, targetLanePosition, interpValue);
                                
                                points.Add(new Point(pointTime, pointPosition, netPosition + pointPosition - startPosition, currentColor));
                            }

                            netPosition += targetLanePosition - lanePosition;
                            lanePosition = targetLanePosition;
                            points.Add(new Point(stackTime, lanePosition, netPosition, currentColor));
                        }
                        else {
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

                            if ((tapNoteFound || holdNoteFound) && points.Count > 0) {
                                float time = Math.Max(stackTime - PREFERRED_STOP_BEFORE_TAP_TIME, 0.5f * (points[points.Count - 1].Time + stackTime));

                                if (targetColor == currentColor)
                                    points.Add(new Point(time, targetLanePosition, netPosition, currentColor));
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

                                    points.Add(new Point(time, lanePosition, netPosition, currentColor));
                                }

                                paths.Add(points);
                                points = new List<Point>();
                            }

                            lanePosition = targetLanePosition;
                            currentColor = targetColor;
                            points.Add(new Point(stackTime, lanePosition, netPosition, targetColor));
                        }
                        
                        previousHoldNote = holdNote;
                        holdNote = null;
                    }

                    if (spinFound)
                        break;

                    if (holdNoteFound)
                        holding = true;
                    else if (holdEndFound)
                        holding = false;

                    holdNoteFound = false;
                    holdPointFound = false;
                    tapNoteFound = false;
                    holdEndFound = false;
                    targetNoteFound = false;
                    stackTime = note.Time;
                    matchesInStack.Clear();
                }
                
                if (type == NoteType.SpinRight || type == NoteType.SpinLeft || type == NoteType.Scratch) {
                    spinFound = true;
                    
                    continue;
                }

                if (type == NoteType.HoldEnd || type == NoteType.Liftoff)
                    holdEndFound = true;
                    
                if (holdNoteFound)
                    continue;
                    
                if (type == NoteType.Hold) {
                    targetLanePosition = note.Column;
                    targetColor = note.Color;
                    targetNoteFound = true;
                    holdNoteFound = true;
                    holdNote = note;
                        
                    continue;
                }
                    
                if (holdPointFound)
                    continue;

                if (holding) {
                    if (type == NoteType.HoldPoint || type == NoteType.HoldEnd || type == NoteType.Liftoff) {
                        targetLanePosition = note.Column;
                        targetColor = currentColor;
                        targetNoteFound = true;
                        holdPointFound = true;
                        holdNote = note;
                    }

                    continue;
                }
                    
                if (tapNoteFound)
                    continue;

                if (type == NoteType.Tap) {
                    targetLanePosition = note.Column;
                    targetColor = note.Color;
                    targetNoteFound = true;
                    tapNoteFound = true;
                        
                    continue;
                }
                    
                if (note.Type == NoteType.Match)
                    matchesInStack.Add(i);
            }
            
            if (points.Count > 0)
                paths.Add(points);

            return paths;
        }

        internal static Point[] Simplify(IList<Point> path, int iterations = -1) {
            if (iterations < 1)
                iterations = SIMPLIFY_ITERATIONS;
            
            var currentPath = path.ToArray();
            var newPath = new Point[path.Count];
            
            for (int i = 0; i < iterations; i++) {
                newPath[0] = currentPath[0];

                for (int j = 1; j < currentPath.Length - 1; j++) {
                    var point = currentPath[j];
                    var previous = currentPath[j - 1];
                    var next = currentPath[j + 1];
                    float pointPosition = point.NetPosition;
                    
                    if (point.CurrentColor != previous.CurrentColor || point.CurrentColor != next.CurrentColor) {
                        newPath[j] = point;

                        continue;
                    }

                    float targetPosition = MathU.Lerp(pointPosition, MathU.Remap(point.Time, previous.Time, next.Time, previous.NetPosition, next.NetPosition), SIMPLIFY_APPROACH_RATE);
                    
                    targetPosition = MathU.Clamp(targetPosition, path[j].NetPosition - 1f, path[j].NetPosition + 1f);
                    newPath[j] = new Point(point.Time, point.LanePosition + targetPosition - pointPosition, targetPosition, point.CurrentColor);
                }

                newPath[newPath.Length - 1] = currentPath[currentPath.Length - 1];
                (currentPath, newPath) = (newPath, currentPath);
            }

            return currentPath;
        }
    }
}