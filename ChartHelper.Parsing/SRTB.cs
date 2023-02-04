using System;
using System.Collections.Generic;
using System.IO;
using ChartHelper.Types;
using Newtonsoft.Json;

namespace ChartHelper.Parsing;

/// <summary>
/// Class containing all JSON-serialized data used by the .srtb format
/// </summary>
public class SRTB {
    [Flags]
    public enum NoteType {
        None = 0,
        Match = 1,
        DrumStart = 2,
        SpinRightStart = 4,
        SpinLeftStart = 8,
        HoldStart = 16,
        SectionContinuationOrEnd = 32,
        Tap = 256,
        Checkpoint = 512,
        TutorialStart = 1024,
        DrumEnd = 2048,
        ScratchStart = 4096
    }
    
    public enum NoteColorType {
        Default,
        NoteA,
        NoteB,
        Beat,
        SpinLeft,
        SpinRight,
        Scratch,
        Ancillary
    }
    
    public enum ClipTransition {
        None,
        FadeOutsideBorder,
        FadeInsideBorder
    }
    
    public enum BeatLengthType {
        Crotchet,
        Quaver,
        Minim
    }

    public enum BeatLengthDotted {
        NoDot,
        Dotted
    }
    
    public enum BPMMarkerType {
        Fixed,
        Interpolated
    }
    
    [Flags]
    public enum TrackPlatformFilter {
        None = 0,
        Standalone = 1,
        iOS = 2,
        Android = 4,
        Switch = 8,
        All = -1
    }
    
    public enum TrackType {
        None = 0,
        Song = 1,
        Tutorial = 4,
        Editor = 8,
        Random = 16,
        CreateCustom = 32,
        EditorFunction = 64
    }
    
    public enum DifficultyType {
        Calibrate,
        Tutorial,
        Easy,
        Normal,
        Hard,
        Expert,
        XD,
        Custom,
        Unknown = 255
    }

    public enum NoteSerializationFormat {
        Original,
        BrotliCompressed,
        BinaryNotes
    }
    
    public class UnityObjectValue {
        [JsonProperty("key")]
        public string Key { get; set; }
            
        [JsonProperty("jsonKey")]
        public string JsonKey { get; set; }
            
        [JsonProperty("fullType")]
        public string FullType { get; set; }
    }

    public class LargeStringValue {
        [JsonProperty("key")]
        public string Key { get; set; }
            
        [JsonProperty("val")]
        public string Val { get; set; }
            
        [JsonProperty("loadedGenerationId")]
        public string LoadedGenerationId { get; set; }
    }

    public class UnityObjectValues {
        [JsonProperty("values")]
        public List<UnityObjectValue> Values { get; set; }
    }

    public class LargeStringValues {
        [JsonProperty("values")]
        public List<LargeStringValue> Values { get; set; }
    }

    public class AssetReference {
        [JsonProperty("bundle")]
        public string Bundle { get; set; }
            
        [JsonProperty("assetName")]
        public string AssetName { get; set; }
            
        [JsonProperty("m_guid")]
        public string Guid { get; set; }
    }

    public class TrackDataAssetReference {
        [JsonProperty("bundle")]
        public string Bundle { get; set; }
            
        [JsonProperty("assetName")]
        public string AssetName { get; set; }
            
        [JsonProperty("m_guid")]
        public string Guid { get; set; }
            
        [JsonProperty("_active")]
        public bool Active { get; set; }
    }

    public class ObjectReference {
        [JsonProperty("m_FileID")]
        public int FileId { get; set; }

        [JsonProperty("m_PathID")]
        public int PathId { get; set; }
    }

    public class TranslationReference {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    public class BackgroundIdReference {
        [JsonProperty("backgroundId")]
        public string BackgroundId { get; set; }
    }

    public class FacetedGroundSettingsIdReference {
        [JsonProperty("m_guid")]
        public string Id_old { set => Id = value; }
        
        [JsonProperty("id")]
        public string Id { get; set; }
    }
        
    public class TutorialObject {
        [JsonProperty("startBeat")]
        public int StartBeat { get; set; }
            
        [JsonProperty("startBar")]
        public int StartBar { get; set; }
            
        [JsonProperty("demoStartBeat")]
        public int DemoStartBeat { get; set; }
            
        [JsonProperty("demoStartBar")]
        public int DemoStartBar { get; set; }
            
        [JsonProperty("demoEndBeat")]
        public int DemoEndBeat { get; set; }
            
        [JsonProperty("demoEndBar")]
        public int DemoEndBar { get; set; }
            
        [JsonProperty("endBeat")]
        public int EndBeat { get; set; }
            
        [JsonProperty("endBar")]
        public int EndBar { get; set; }

        [JsonProperty("noteRequirementType")]
        public NoteType NoteRequirementType { get; set; }
            
        [JsonProperty("noteRequirementCount")]
        public int NoteRequirementCount { get; set; }
            
        [JsonProperty("mustPassNoteRequirementTest")]
        public bool MustPassNoteRequirementTest { get; set; }
            
        [JsonProperty("restartDemoAfterThisAttemptCount")]
        public int RestartDemoAfterThisAttemptCount { get; set; }
            
        [JsonProperty("tutorialTitle")]
        public TranslationReference TutorialTitle { get; set; }
            
        [JsonProperty("tutorialInstruction")]
        public TranslationReference TutorialInstruction { get; set; }
            
        [JsonProperty("objectPrefab")]
        public ObjectReference ObjectPrefab { get; set; }
    }

    public class TutorialText {
        [JsonProperty("translation")]
        public TranslationReference Translation { get; set; }
            
        [JsonProperty("noteColorType")]
        public NoteColorType NoteColorType { get; set; }

        [JsonProperty("colorTypeString")]
        public string ColorTypeString { get; set; }

        [JsonProperty("time")]
        public float Time { get; set; }
    }

    public class ClipData {
        [JsonProperty("clipIndex")]
        public int ClipIndex { get; set; }
            
        [JsonProperty("startBar")]
        public int StartBar { get; set; }
            
        [JsonProperty("endBar")]
        public int EndBar { get; set; }
            
        [JsonProperty("transitionIn")]
        public ClipTransition TransitionIn { get; set; }
            
        [JsonProperty("transitionInValue")]
        public float TransitionInValue { get; set; }
            
        [JsonProperty("transitionInOffset")]
        public float TransitionInOffset { get; set; }
            
        [JsonProperty("transitionOut")]
        public ClipTransition TransitionOut { get; set; }
            
        [JsonProperty("transitionOutValue")]
        public float TransitionOutValue { get; set; }
            
        [JsonProperty("transitionOutOffset")]
        public float TransitionOutOffset { get; set; }
    }

    public class Note {
        [JsonProperty("time")]
        public float Time { get; set; }
            
        [JsonProperty("type")]
        public int Type { get; set; }
            
        [JsonProperty("colorIndex")]
        public int ColorIndex { get; set; }
            
        [JsonProperty("column")]
        public int Column { get; set; }
            
        [JsonProperty("m_size")]
        public int Size { get; set; }
    }

    public class NoteBinary {
        [JsonProperty("tk")]
        public int Tk { get; set; }
        
        [JsonProperty("tp")]
        public int Tp { get; set; }
        
        [JsonProperty("c")]
        public int C { get; set; }
        
        [JsonProperty("p")]
        public int P { get; set; }
        
        [JsonProperty("s")]
        public int S { get; set; }
    }

    public class RewindSection { }

    public class TimeSignatureMarker {
        [JsonProperty("startingBeat")]
        public int StartingBeat { get; set; }
            
        [JsonProperty("ticksPerBar")]
        public int TicksPerBar { get; set; }
            
        [JsonProperty("tickDivisor")]
        public int TickDivisor { get; set; }
            
        [JsonProperty("beatLengthType")]
        public BeatLengthType BeatLengthType { get; set; }
            
        [JsonProperty("beatLengthDotted")]
        public BeatLengthDotted BeatLengthDotted { get; set; }
    }

    public class BPMMarker {
        [JsonProperty("beatLength")]
        public float BeatLength { get; set; }
            
        [JsonProperty("clipTime")]
        public float ClipTime { get; set; }
            
        [JsonProperty("type")]
        public BPMMarkerType Type { get; set; }
    }

    public class CuePoint {
        [JsonProperty("name")]
        public string Name { get; set; }
            
        [JsonProperty("time")]
        public float Time { get; set; }
    }

    public class Color {
        [JsonProperty("r")]
        public float R { get; set; }
            
        [JsonProperty("g")]
        public float G { get; set; }
            
        [JsonProperty("b")]
        public float B { get; set; }
            
        [JsonProperty("a")]
        public float A { get; set; }
    }

    public class IntRange {
        [JsonProperty("min")]
        public int Min { get; set; }
            
        [JsonProperty("max")]
        public int Max { get; set; }
    }

    public class TrackInfo {
        [JsonProperty("trackOrder")]
        public int TrackOrder { get; set; }
        
        [JsonProperty("trackId")]
        public int TrackId { get; set; }
        
        [JsonProperty("albumArtReference")]
        public AssetReference AlbumArtReference { get; set; }
        
        [JsonProperty("backgroundId")]
        public BackgroundIdReference BackgroundId { get; set; }
        
        [JsonProperty("backgroundColoring")]
        public FacetedGroundSettingsIdReference BackgroundColoring { get; set; }
        
        [JsonProperty("artistName")]
        public string ArtistName { get; set; }
        
        [JsonProperty("featArtists")]
        public string FeatArtists { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }
        
        [JsonProperty("trackLabel")]
        public string TrackLabel { get; set; }
        
        [JsonProperty("charter")]
        public string Charter { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("titleTextColor")]
        public Color TitleTextColor { get; set; }
        
        [JsonProperty("subtitleTextColor")]
        public Color SubtitleTextColor { get; set; }
        
        [JsonProperty("titleOffsetY")]
        public float TitleOffsetY { get; set; }
        
        [JsonProperty("spotifyLink")]
        public string SpotifyLink { get; set; }
        
        [JsonProperty("appleMusicLink")]
        public string AppleMusicLink { get; set; }
        
        [JsonProperty("difficulties")]
        public List<TrackDataAssetReference> Difficulties { get; set; }
        
        [JsonProperty("platformFilter")]
        public TrackPlatformFilter PlatformFilter { get; set; }
        
        [JsonProperty("trackType")]
        public TrackType TrackType { get; set; }
        
        [JsonProperty("editorFunction")]
        public string EditorFunction { get; set; }
            
        [JsonProperty("allowCustomLeaderboardCreation")]
        public bool AllowCustomLeaderboardCreation { get; set; }

        public bool HasDifficulty(DifficultyType difficulty) => Difficulties[difficulty - DifficultyType.Easy].Active;
    }

    public class TrackData {
        [JsonProperty("revisionVersion")]
        public int RevisionVersion { get; set; }
            
        [JsonProperty("compatibilityVersion")]
        public int CompatabilityVersion { get; set; }
        
        [JsonProperty("noteSerializationFormat")]
        public NoteSerializationFormat NoteSerializationFormat { get; set; }
            
        [JsonProperty("difficultyRating")]
        public int DifficultyRating { get; set; }
            
        [JsonProperty("previewLoopBars")]
        public IntRange PreviewLoopBars { get; set; }
            
        [JsonProperty("goBeatOffsetFromFirstNote")]
        public float GoBeatOffsetFromFirstNote { get; set; }
            
        [JsonProperty("difficultyType")]
        public DifficultyType DifficultyType { get; set; }
            
        [JsonProperty("isTutorial")]
        public bool IsTutorial { get; set; }
            
        [JsonProperty("tutorialTitleTranslation")]
        public TranslationReference TutorialTitleTranslation { get; set; }

        [JsonProperty("clipInfoAssetReferences")]
        public List<AssetReference> ClipInfoAssetReferences { get; set; }

        [JsonProperty("backgroundId")]
        public BackgroundIdReference BackgroundId { get; set; }

        [JsonProperty("background")]
        public AssetReference Background { get; set; }

        [JsonProperty("groundSettingsToUse")]
        public AssetReference GroundSettingsToUse { get; set; }

        [JsonProperty("groundSettingsOverTime")]
        public AssetReference GroundSettingsOverTime { get; set; }

        [JsonProperty("splinePathData")]
        public ObjectReference SplinePathData { get; set; }

        [JsonProperty("_feverTime")]
        public ObjectReference FeverTime { get; set; }
            
        [JsonProperty("tutorialObjects")]
        public List<TutorialObject> TutorialObjects { get; set; }

        [JsonProperty("clipData")]
        public List<ClipData> ClipData { get; set; }
            
        [JsonProperty("notes")]
        public List<Note> Notes { get; set; }
        
        [JsonProperty("binaryNotes")]
        public List<NoteBinary> BinaryNotes { get; set; }
        
        [JsonProperty("rewindSections")]
        public List<RewindSection> RewindSections { get; set; }
            
        [JsonProperty("lastEditedOnDate")]
        public string LastEditedOnDate { get; set; }
    }

    public class ClipInfo {
        [JsonProperty("timeSignatureMarkers")]
        public List<TimeSignatureMarker> TimeSignatureMarkers { get; set; }

        [JsonProperty("bpmMarkers")]
        public List<BPMMarker> BpmMarkers { get; set; }

        [JsonProperty("cuePoints")]
        public List<CuePoint> CuePoints { get; set; }

        [JsonProperty("clipAssetReference")]
        public AssetReference ClipAssetReference { get; set; }
    }

    [JsonProperty("unityObjectValuesContainer")]
    public UnityObjectValues UnityObjectValuesContainer { get; set; }

    [JsonProperty("largeStringValuesContainer")]
    public LargeStringValues LargeStringValuesContainer { get; set; }
        
    [JsonProperty("clipInfoCount")]
    public int ClipInfoCount { get; set; }
        
    /// <summary>
    /// Sets an srtb's track info
    /// </summary>
    /// <param name="trackInfo">The new track info</param>
    public void SetTrackInfo(TrackInfo trackInfo) => SetLargeStringValue("SO_TrackInfo_TrackInfo", trackInfo);

    /// <summary>
    /// Sets an srtb's track data with a given index
    /// </summary>
    /// <param name="index">The index of the track data to set</param>
    /// <param name="trackData">The new track data</param>
    public void SetTrackData(int index, TrackData trackData) => SetLargeStringValue($"SO_TrackData_TrackData_{index}", trackData);
    /// <summary>
    /// Sets an srtb's track data with a given difficulty
    /// </summary>
    /// <param name="difficultyType">The difficulty type of the track data to set</param>
    /// <param name="trackData">The new track data</param>
    public void SetTrackData(DifficultyType difficultyType, TrackData trackData) => SetTrackData(difficultyType - DifficultyType.Easy, trackData);

    /// <summary>
    /// Sets an srtb's clip info with a given index
    /// </summary>
    /// <param name="index">The index of the clip info to set</param>
    /// <param name="clipInfo">The new clip info</param>
    public void SetClipInfo(int index, ClipInfo clipInfo) => SetLargeStringValue($"SO_ClipInfo_ClipInfo_{index}", clipInfo);

    /// <summary>
    /// Serializes the srtb to a file at a given path
    /// </summary>
    /// <param name="path">The path to serialize to</param>
    public void SerializeToFile(string path) {
        using var writer = new StreamWriter(path);
        
        JsonSerializer.Create().Serialize(writer, this);
    }

    /// <summary>
    /// Serializes the srtb to a string
    /// </summary>
    /// <returns>The serialized string</returns>
    public string Serialize() => JsonConvert.SerializeObject(this);

    /// <summary>
    /// Gets an srtb's track info
    /// </summary>
    /// <returns>The track info</returns>
    public TrackInfo GetTrackInfo() => GetLargeStringValue<TrackInfo>("SO_TrackInfo_TrackInfo");
        
    /// <summary>
    /// Gets an srtb's track data with a given index
    /// </summary>
    /// <param name="index">The index of the track data to get</param>
    /// <returns>The track data</returns>
    public TrackData GetTrackData(int index) => GetLargeStringValue<TrackData>($"SO_TrackData_TrackData_{index}");
    /// <summary>
    /// Gets an srtb's track data with a given difficulty
    /// </summary>
    /// <param name="difficultyType">The difficulty type of the track data to get</param>
    /// <returns>The track data</returns>
    public TrackData GetTrackData(DifficultyType difficultyType) => GetTrackData(difficultyType - DifficultyType.Easy);
        
    /// <summary>
    /// Gets an srtb's clip info with a given index
    /// </summary>
    /// <param name="index">The index of the clip info to get</param>
    /// <returns>The clip info</returns>
    public ClipInfo GetClipInfo(int index) => GetLargeStringValue<ClipInfo>($"SO_ClipInfo_ClipInfo_{index}");

    /// <summary>
    /// Deserializes an srtb from a file at a given path
    /// </summary>
    /// <param name="path">The path to deserialize from</param>
    /// <returns>The deserialized srtb</returns>
    public static SRTB DeserializeFromFile(string path) {
        if (!File.Exists(path))
            return null;
        
        using var reader = new StreamReader(path);

        return (SRTB) JsonSerializer.Create().Deserialize(reader, typeof(SRTB));
    }
    
    /// <summary>
    /// Deserializes an srtb from a string
    /// </summary>
    /// <param name="text">The string to deserialize</param>
    /// <returns>The deserialized srtb</returns>
    public static SRTB Deserialize(string text) => JsonConvert.DeserializeObject<SRTB>(text);

    private void SetLargeStringValue(string key, object value) {
        foreach (var pair in LargeStringValuesContainer.Values) {
            if (pair.Key != key)
                continue;

            pair.Val = JsonConvert.SerializeObject(value);

            return;
        }
    }

    private T GetLargeStringValue<T>(string key) where T : class {
        foreach (var pair in LargeStringValuesContainer.Values) {
            if (pair.Key != key)
                continue;

            return JsonConvert.DeserializeObject<T>(pair.Val);
        }

        return null;
    }
}