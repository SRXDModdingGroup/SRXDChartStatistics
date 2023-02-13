using System.Collections.Generic;

namespace ModelTraining; 

public class DataElement {
    public int Id { get; }
    
    public string Title { get; }
    
    public List<double> RatingData { get; }

    public DataElement(int id, string title, List<double> ratingData) {
        Id = id;
        Title = title;
        RatingData = ratingData;
    }
}