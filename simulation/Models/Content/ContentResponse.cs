namespace simulation.Models.Content;

/// <summary>
/// Antwort von der ContentApi
/// </summary>
public class ContentResponse
{
    /// <summary>
    /// Ean
    /// </summary>
    public string Ean { get; set; } = null!;

    /// <summary>
    /// Attribute des angefragten Artikels
    /// </summary>
    public ContentAttributes? Attributes { get; set; }
}