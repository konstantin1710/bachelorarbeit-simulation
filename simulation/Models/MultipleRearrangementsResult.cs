namespace simulation.Models;

public class MultipleRearrangementsResult
{
    public MultipleRearrangementsResult()
    {
        HighzoneGroundzone = new RearrangementsResult();
        GroundzoneGroundzone = new RearrangementsResult();
    }
    public RearrangementsResult HighzoneGroundzone { get; set; }
    public RearrangementsResult GroundzoneGroundzone { get; set; }

    public void Add(MultipleRearrangementsResult other)
    {
        HighzoneGroundzone.Count += other.HighzoneGroundzone.Count;
        HighzoneGroundzone.Length += other.HighzoneGroundzone.Length;
        GroundzoneGroundzone.Count += other.GroundzoneGroundzone.Count;
        GroundzoneGroundzone.Length += other.GroundzoneGroundzone.Length;
    }
}