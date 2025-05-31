using System.ComponentModel.DataAnnotations;
namespace HuffmanFileCompressor.App.Model;

public class Archives
{
    public int Id { get; set; }
    [StringLength(255)]
    public required string Name { get; set; }
    [StringLength(255)]
    public required string Path { get; set; }
    [StringLength(255)]
    public required  string Extension { get; set; }
    public int OriginalSize { get; init; }
    public int CompressedSize { get; init; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}
