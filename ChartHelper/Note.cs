using System.Collections.Generic;

namespace ChartHelper {
    /// <summary>
    /// Contains data for a single note
    /// </summary>
    public class Note {
        private static readonly Dictionary<int, NoteTypeRaw> NOTE_TYPE_VALUES = new Dictionary<int, NoteTypeRaw> {
            { 0, NoteTypeRaw.Match },
            { 1, NoteTypeRaw.Beat },
            { 2, NoteTypeRaw.SpinRight },
            { 3, NoteTypeRaw.SpinLeft },
            { 4, NoteTypeRaw.Hold },
            { 5, NoteTypeRaw.HoldPoint },
            { 8, NoteTypeRaw.Tap },
            { 11, NoteTypeRaw.BeatRelease },
            { 12, NoteTypeRaw.Scratch }
        };
        
        /// <summary>
        /// The time of the note
        /// </summary>
        public float Time { get; }
        /// <summary>
        /// The type of the note, as specified by the srtb
        /// </summary>
        public NoteTypeRaw TypeRaw { get; }
        /// <summary>
        /// The specific type of the note
        /// </summary>
        public NoteType Type { get; internal set; }
        /// <summary>
        /// The color of the note
        /// </summary>
        public NoteColor Color { get; internal set; }
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
        public int StartIndex { get; internal set; } = -1;
        /// <summary>
        /// The index of the end of a sustained note
        /// </summary>
        public int EndIndex { get; internal set; } = -1;
        /// <summary>
        /// True if the note ends a spin or scratch zone
        /// </summary>
        public bool IsSpinEnd { get; internal set; }
        /// <summary>
        /// True if a spin or scratch sill auto snap to this note
        /// </summary>
        public bool IsAutoSnap { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="time">The time of the note</param>
        /// <param name="typeRaw">The type of the note</param>
        /// <param name="color">The color of the note</param>
        /// <param name="column">The lane of the note. Value increases to the left</param>
        /// <param name="curveType">The curve type of the note. Also may specify if the note is a liftoff or hard beat release</param>
        internal Note(float time, NoteTypeRaw typeRaw, NoteColor color = NoteColor.Blue, int column = 0, CurveType curveType = CurveType.Cosine) {
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

        internal Note(float time, int type, int color = 0, int column = 0, int curveType = 0)
            : this(time, NOTE_TYPE_VALUES.TryGetValue(type, out var newType) ? newType : NoteTypeRaw.Experimental, (NoteColor) color, column, curveType == 0 ? CurveType.Cosine : (CurveType) curveType) { }
    }
}