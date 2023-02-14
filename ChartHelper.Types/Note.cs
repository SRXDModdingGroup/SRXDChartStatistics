using System.Collections.Generic;

namespace ChartHelper.Types; 

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
    public NoteColor Color { get; }
    /// <summary>
    /// The lane of the note. Value increases to the left
    /// </summary>
    public int Column { get; }
    /// <summary>
    /// The specific curve type of the note
    /// </summary>
    public CurveType CurveType { get; }
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
        Note lastHold = null;
        Note lastBeat = null;
        Note lastSpin = null;
        bool readyToSnap = false;

        for (int i = 0; i < notes.Count; i++) {
            var note = notes[i];
            bool skip = false;

            switch (note.TypeRaw) {
                case NoteTypeRaw.SpinRight:
                case NoteTypeRaw.SpinLeft:
                case NoteTypeRaw.Scratch:
                    EndHold();
                    EndSpin(note, i);
                    lastSpin = note;
                    readyToSnap = true;

                    break;
                case NoteTypeRaw.Beat:
                    lastBeat = note;

                    break;
                case NoteTypeRaw.Tap:
                case NoteTypeRaw.Match:
                    EndSpin(note, i);

                    if (readyToSnap) {
                        note.IsAutoSnap = true;
                        readyToSnap = false;
                    }

                    break;
                case NoteTypeRaw.BeatRelease:
                    if (lastBeat == null) {
                        skip = true;

                        break;
                    }

                    lastBeat.EndIndex = i;
                    lastBeat = null;

                    break;
                case NoteTypeRaw.Hold:
                    EndHold();
                    EndSpin(note, i);
                    lastHold = note;
                    
                    if (readyToSnap) {
                        note.IsAutoSnap = true;
                        readyToSnap = false;
                    }

                    break;
                case NoteTypeRaw.HoldPoint:
                    if (lastSpin != null) {
                        EndSpin(note, i);
                        note.Type = NoteType.SpinEnd;
                    }
                    else if (lastHold != null)
                        lastHold.EndIndex = i;
                    else
                        skip = true;

                    break;
            }

            if (!skip)
                continue;
                
            notes.RemoveAt(i);
            i--;
        }

        EndHold();
            
        void EndHold() {
            if (lastHold == null || lastHold.EndIndex < 0)
                return;
            
            var holdPoint = notes[lastHold.EndIndex];
                
            if (holdPoint.CurveType == CurveType.CurveOut)
                holdPoint.Type = NoteType.Liftoff;
            else
                holdPoint.Type = NoteType.HoldEnd;

            lastHold = null;
        }

        void EndSpin(Note note, int index) {
            if (lastSpin == null)
                return;
                
            note.IsSpinEnd = true;
            lastSpin.EndIndex = index;
            lastSpin = null;
        }
    }
}