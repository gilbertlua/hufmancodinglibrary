namespace HuffmanFileCompressor.App.Model;

public class Archives
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    public required string Path { get; set; }
    public int OriginalSize { get; set; }
    public int CompressedSize { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}