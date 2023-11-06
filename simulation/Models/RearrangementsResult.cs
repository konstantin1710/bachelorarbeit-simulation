namespace simulation.Models;

public class RearrangementsResult
{
    public int Count { get; set; }
    public double Length { get; set; }

    public void Add(RearrangementsResult other)
    {
        Count += other.Count;
        Length += other.Length;
    }
}