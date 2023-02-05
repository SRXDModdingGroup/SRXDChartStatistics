using System.Collections.Generic;

namespace ChartHelper.Types {
    /// <summary>
    /// Contains data for a single note
    /// </summary>
    public class Note {
        /// <summary>
        /// The time of the note
        /// </summary>
        public double Time { get; }
        /// <summary>
        /// The type of the note, as specified by the srtb
        /// </summary>
        public NoteTypeRaw TypeRaw { get; }
        /// <summary>
        /// The specific type of the note
        /// </summary>
        public NoteType Type { get; private set; }
        /// <summary>
        /// The color of the note
        /// </summary>
        public NoteColor Color { get; private set; }
        /// <summary>
        /// The lane of the note. Value increases to the left
        /// </summary>
        public int Column { get; }
        /// <summary>
        /// The specific curve type of the note
        /// </summary>
        public CurveType CurveType { get; }
        /// <summary>
        /// The index of the start of a sustained note
        /// </summary>
        public int StartIndex { get; private set; } = -1;
        /// <summary>
        /// The index of the end of a sustained note
        /// </summary>
        public int EndIndex { get; private set; } = -1;
        /// <summary>
        /// True if the note ends a spin or scratch zone
        /// </summary>
        public bool IsSpinEnd { get; private set; }
        /// <summary>
        /// True if a spin or scratch sill auto snap to this note
        /// </summary>
        public bool IsAutoSnap { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="time">The time of the note</param>
        /// <param name="typeRaw">The type of the note</param>
        /// <param name="color">The color of the note</param>
        /// <param name="column">The lane of the note. Value increases to the left</param>
        /// <param name="curveType">The curve type of the note. Also may specify if the note is a liftoff or hard beat release</param>
        public Note(double time, NoteTypeRaw typeRaw, NoteColor color = NoteColor.Blue, int column = 0, CurveType curveType = CurveType.Cosine) {
            Time = time;
            TypeRaw = typeRaw;
            CurveType = curveType;
            Color = color;
            Column = column;
            
            switch (typeRaw) {
                case NoteTypeRaw.Match:
                    Type = NoteType.Match;
                    break;
                case NoteTypeRaw.Beat:
                    Type = NoteType.Beat;
                    break;
                case NoteTypeRaw.SpinRight:
                    Type = NoteType.SpinRight;
                    break;
                case NoteTypeRaw.SpinLeft:
                    Type = NoteType.SpinLeft;
                    break;
                case NoteTypeRaw.Hold:
                    Type = NoteType.Hold;
                    break;
                case NoteTypeRaw.HoldPoint:
                    Type = NoteType.HoldPoint;
                    break;
                case NoteTypeRaw.Tap:
                    Type = NoteType.Tap;
                    break;
                case NoteTypeRaw.BeatRelease:
                    if (curveType == CurveType.Cosine)
                        Type = NoteType.BeatReleaseHard;
                    else
                        Type = NoteType.BeatReleaseSoft;

                    break;
                case NoteTypeRaw.Scratch:
                    Type = NoteType.Scratch;
                    break;
                case NoteTypeRaw.Experimental:
                    Type = NoteType.Experimental;
                    break;
            }
        }

        /// <summary>
        /// Adds detailed information to a set of notes
        /// </summary>
        /// <param name="notes">The notes to apply</param>
        public static void ApplyDetailedData(IList<Note> notes) {
            int sustainedNoteStartIndex = -1;
            int lastHoldPointIndex = -1;
            int lastBeatIndex = -1;
            bool holding = false;
            bool spinning = false;
            bool beatHolding = false;
            bool readyToSnap = false;

            for (int i = 0; i < notes.Count; i++) {
                var note = notes[i];
                bool skip = false;

                switch (note.TypeRaw) {
                    case NoteTypeRaw.SpinRight:
                    case NoteTypeRaw.SpinLeft:
                    case NoteTypeRaw.Scratch:
                        EndHold();
                        EndSpin(i, note);
                        sustainedNoteStartIndex = i;
                        spinning = true;
                        readyToSnap = true;

                        break;
                    case NoteTypeRaw.Beat:
                        lastBeatIndex = i;
                        beatHolding = true;

                        break;
                    case NoteTypeRaw.Tap:
                    case NoteTypeRaw.Match:
                        EndSpin(i, note);

                        if (readyToSnap) {
                            note.IsAutoSnap = true;
                            readyToSnap = false;
                        }

                        break;
                    case NoteTypeRaw.BeatRelease:
                        if (!beatHolding) {
                            skip = true;

                            break;
                        }

                        beatHolding = false;
                        note.StartIndex = lastBeatIndex;

                        if (lastBeatIndex >= 0)
                            notes[lastBeatIndex].EndIndex = i;

                        break;
                    case NoteTypeRaw.Hold:
                        EndHold();
                        EndSpin(i, note);

                        if (readyToSnap) {
                            note.IsAutoSnap = true;
                            readyToSnap = false;
                        }

                        holding = true;
                        sustainedNoteStartIndex = i;
                        lastHoldPointIndex = -1;

                        break;
                    case NoteTypeRaw.HoldPoint:
                        if (spinning) {
                            spinning = false;
                            note.Type = NoteType.SpinEnd;
                        }
                        else if (holding) {
                            lastHoldPointIndex = i;
                            note.Color = notes[sustainedNoteStartIndex].Color;
                        }
                        else {
                            skip = true;

                            break;
                        }

                        note.StartIndex = sustainedNoteStartIndex;

                        if (sustainedNoteStartIndex > -1)
                            notes[sustainedNoteStartIndex].EndIndex = i;

                        sustainedNoteStartIndex = i;

                        break;
                }

                if (!skip)
                    continue;
                
                notes.RemoveAt(i);
                i--;
            }

            EndHold();
            
            void EndHold() {
                if (!holding || lastHoldPointIndex < 0)
                    return;
                
                var lastHoldPoint = notes[lastHoldPointIndex];
                    
                if (lastHoldPoint.CurveType == CurveType.CurveOut)
                    lastHoldPoint.Type = NoteType.Liftoff;
                else
                    lastHoldPoint.Type = NoteType.HoldEnd;

                holding = false;
            }

            void EndSpin(int i, Note note) {
                if (!spinning)
                    return;
                
                spinning = false;
                note.IsSpinEnd = true;
                
                if (sustainedNoteStartIndex > -1)
                    notes[sustainedNoteStartIndex].EndIndex = i;
            }
        }
    }
}