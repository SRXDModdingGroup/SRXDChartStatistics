using Newtonsoft.Json;

namespace ModelTraining; 

public class DataElement {
    [JsonProperty(PropertyName = "id")]
    public int Id { get; }
    
    [JsonProperty(PropertyName = "title")]
    public string Title { get; }
    
    [JsonProperty(PropertyName = "ratingData")]
    public double[] RatingData { get; }
    
    public DataElement(int id, string title, double[] ratingData) {
        Id = id;
        Title = title;
        RatingData = ratingData;
    }
}