namespace ChartHelper.Types; 

/// <summary>
/// The type of a note, as specified by the srtb
/// </summary>
public enum NoteTypeRaw {
    Match = 0,
    Beat = 1,
    SpinRight = 2,
    SpinLeft = 3,
    Hold = 4,
    HoldPoint = 5,
    Tap = 8,
    Experimental = 10,
    BeatRelease = 11,
    Scratch = 12
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
    Experimental
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