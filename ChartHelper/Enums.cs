namespace ChartHelper {
    /// <summary>
    /// The type of a note, as specified by the srtb
    /// </summary>
    public enum NoteTypeRaw {
        Match,
        Beat,
        SpinRight,
        SpinLeft,
        Hold,
        HoldPoint,
        Tap,
        BeatRelease,
        Scratch,
        Experimental
    }

    /// <summary>
    /// The specific type of a note
    /// </summary>
    public enum NoteType {
        Match,
        Beat,
        SpinRight,
        SpinLeft,
        Hold,
        HoldPoint,
        HoldEnd,
        Liftoff,
        SpinEnd,
        Tap,
        BeatReleaseSoft,
        BeatReleaseHard,
        Scratch,
        Experimental,
    }

    /// <summary>
    /// The curve type of a note
    /// </summary>
    public enum CurveType {
        /// <summary>
        /// Also specifies regular hold ends and hard beat releases
        /// </summary>
        Cosine = 1,
        /// <summary>
        /// Also specifies liftoffs and soft beat releases
        /// </summary>
        CurveOut,
        CurveIn,
        Linear,
        Angular
    }

    /// <summary>
    /// The color of a note
    /// </summary>
    public enum NoteColor {
        Blue,
        Red
    }
    
    /// <summary>
    /// The difficulty type of a track
    /// </summary>
    public enum Difficulty {
        Easy,
        Normal,
        Hard,
        Expert,
        XD
    }
}