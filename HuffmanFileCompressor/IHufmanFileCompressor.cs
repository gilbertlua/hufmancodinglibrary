using System.IO;

namespace HuffmanFileCompressor
{
    public interface IHuffmanFileCompressor
    {
        public CompressionResult CompressFile(string inputFilePath, string? outputFilePath = null);
        public DecompressionResult DecompressFile(string inputFilePath, string? outputFilePath = null);
        
    }
}