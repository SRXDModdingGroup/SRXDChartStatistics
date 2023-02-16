using Newtonsoft.Json;

namespace ModelTraining; 

public class DataElement {
    [JsonProperty(PropertyName = "id")]
    public int Id { get; }
    
    [JsonProperty(PropertyName = "title")]
    public string Title { get; }
    
    [JsonProperty(PropertyName = "difficulty")]
    public double Difficulty { get; set; }
    
    [JsonProperty(PropertyName = "ratingData")]
    public double[] RatingData { get; }
    
    public DataElement(int id, string title, double difficulty, double[] ratingData) {
        Id = id;
        Title = title;
        Difficulty = difficulty;
        RatingData = ratingData;
    }
}