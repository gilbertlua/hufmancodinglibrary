
// HuffmanFileCompressor.Tests/HuffmanFileCompressorTests.cs
using Xunit;
using System.IO;
using System.Text;
using HuffmanFileCompressor; // Reference the library namespace

namespace HuffmanFileCompressor.Tests
{
    public class HuffmanFileCompressorTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly HuffmanFileCompressor _compressor;

        public HuffmanFileCompressorTests()
        {
            // Create a unique temporary directory for each test run
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Set default output directory for tests
            HuffmanFileCompressor.CompressionSettings.DefaultOutputDirectory = _testDirectory;
            HuffmanFileCompressor.CompressionSettings.AcceptedFileExtensions = null; // Accept all for tests unless specified

            _compressor = new HuffmanFileCompressor();
        }

        public void Dispose()
        {
            // Clean up the temporary directory after all tests in this class are run
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            // Reset settings to avoid affecting other tests if they were modified
            HuffmanFileCompressor.CompressionSettings.DefaultOutputDirectory = null;
            HuffmanFileCompressor.CompressionSettings.AcceptedFileExtensions = null;
        }

        private string CreateTestFile(string fileName, string content)
        {
            string filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content, Encoding.UTF8);
            return filePath;
        }

        private string CreateBinaryTestFile(string fileName, byte[] content)
        {
            string filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllBytes(filePath, content);
            return filePath;
        }

        [Fact]
        public void CompressAndDecompress_SmallTextFile_ShouldMatchOriginal()
        {
            // Arrange
            string originalContent = "This is a small test file for Huffman compression This is a small test file for Huffman compressionThis is a small test file for Huffman compression This is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compressionThis is a small test file for Huffman compression";
            string inputFilePath = CreateTestFile("small_test.txt", originalContent);
            string compressedFilePath = Path.Combine(_testDirectory, "small_test.huff");
            string decompressedFilePath = Path.Combine(_testDirectory, "small_test.decompressed");

            // Act - Compress
            CompressionResult compressionResult = _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert - Compression
            Assert.True(compressionResult.IsSuccess);
            Assert.True(File.Exists(compressedFilePath));
            Assert.True(compressionResult.OriginalSize > compressionResult.CompressedSize); // Expect compression for text
             // Ensure some compression occurred

            // Act - Decompress
            DecompressionResult decompressionResult = _compressor.DecompressFile(compressedFilePath, decompressedFilePath);

            // Assert - Decompression
            Assert.True(decompressionResult.IsSuccess);
            Assert.True(File.Exists(decompressedFilePath));
            Assert.Equal(compressionResult.OriginalSize, decompressionResult.DecompressedSize);

            // Verify content
            string decompressedContent = File.ReadAllText(decompressedFilePath, Encoding.UTF8);
            Assert.Equal(originalContent, decompressedContent);
        }

        [Fact]
        public void CompressAndDecompress_EmptyFile_ShouldHandleGracefully()
        {
            // Arrange
            string originalContent = "";
            string inputFilePath = CreateTestFile("empty_file.txt", originalContent);
            string compressedFilePath = Path.Combine(_testDirectory, "empty_file.huff");
            string decompressedFilePath = Path.Combine(_testDirectory, "empty_file.decompressed");
            int emptyFileSize = 3; // Size of an empty file
            // Act - Compress
            CompressionResult compressionResult = _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert - Compression (empty file might not compress much, but should succeed)
            Assert.True(compressionResult.IsSuccess);
            Assert.True(File.Exists(compressedFilePath));
            Assert.Equal(emptyFileSize, compressionResult.OriginalSize);
            // Compressed size might be non-zero due to metadata (frequency table, original size)
            Assert.True(compressionResult.CompressedSize >= 0);

            // Act - Decompress
            DecompressionResult decompressionResult = _compressor.DecompressFile(compressedFilePath, decompressedFilePath);

            // Assert - Decompression
            Assert.True(decompressionResult.IsSuccess);
            Assert.True(File.Exists(decompressedFilePath));
            Assert.Equal(emptyFileSize, decompressionResult.DecompressedSize);

            // Verify content
            string decompressedContent = File.ReadAllText(decompressedFilePath, Encoding.UTF8);
            Assert.Equal(originalContent, decompressedContent);
        }

        [Fact]
        public void CompressAndDecompress_BinaryFile_ShouldMatchOriginal()
        {
            const int desiredSize = 1024 * 1024; // 1 MB

            byte[] originalBytes = new byte[desiredSize]; // Declare the array with the new size

            for (int i = 0; i < desiredSize; i++)
            {
                originalBytes[i] = (byte)(i % 16);
            }
            string inputFilePath = CreateBinaryTestFile("binary_test.bin", originalBytes);
            string compressedFilePath = Path.Combine(_testDirectory, "binary_test.huff");
            string decompressedFilePath = Path.Combine(_testDirectory, "binary_test.decompressed");

            // Act - Compress
            CompressionResult compressionResult = _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert - Compression
            Assert.True(compressionResult.IsSuccess);
            Assert.True(File.Exists(compressedFilePath));
            // Expect some compression due to repeating patterns
            Assert.True(compressionResult.OriginalSize > compressionResult.CompressedSize);

            // Act - Decompress
            DecompressionResult decompressionResult = _compressor.DecompressFile(compressedFilePath, decompressedFilePath);

            // Assert - Decompression
            Assert.True(decompressionResult.IsSuccess);
            Assert.True(File.Exists(decompressedFilePath));
            Assert.Equal(compressionResult.OriginalSize, decompressionResult.DecompressedSize);

            // Verify content
            byte[] decompressedBytes = File.ReadAllBytes(decompressedFilePath);
            Assert.Equal(originalBytes, decompressedBytes);
        }

        [Fact]
        public void Compress_NonExistentInputFile_ShouldReturnFailure()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "non_existent.txt");
            string outputFilePath = Path.Combine(_testDirectory, "output.huff");

            // Act
            CompressionResult result = _compressor.CompressFile(nonExistentPath, outputFilePath);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("invalid or file does not exist", result.ErrorMessage);
        }

        [Fact]
        public void Decompress_NonExistentInputFile_ShouldReturnFailure()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "non_existent.huff");
            string outputFilePath = Path.Combine(_testDirectory, "output.decompressed");

            // Act
            DecompressionResult result = _compressor.DecompressFile(nonExistentPath, outputFilePath);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("invalid or file does not exist", result.ErrorMessage);
        }

        [Fact]
        public void Compress_UnsupportedFileType_ShouldReturnFailure()
        {
            // Arrange
            HuffmanFileCompressor.CompressionSettings.AcceptedFileExtensions = new string[] { ".txt" };
            string originalContent = "This is a PDF simulation.";
            string inputFilePath = CreateTestFile("document.pdf", originalContent); // .pdf is not accepted
            string compressedFilePath = Path.Combine(_testDirectory, "document.huff");

            // Act
            CompressionResult result = _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not accepted for compression", result.ErrorMessage);
        }

        [Fact]
        public void Compress_SupportedFileType_ShouldSucceed()
        {
            // Arrange
            HuffmanFileCompressor.CompressionSettings.AcceptedFileExtensions = new string[] { ".txt", ".log" };
            string originalContent = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla at sem sodales urna venenatis sodales. Etiam feugiat ante massa, eget tristique nisi tempor sagittis. Donec ac metus metus. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Aenean urna arcu, aliquam fringilla iaculis quis, bibendum nec sem. Ut orci lectus, efficitur vestibulum porttitor quis, laoreet et diam. Nullam ac auctor diam. Maecenas sollicitudin tincidunt sapien, id volutpat odio aliquam ut. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam erat volutpat. Donec sit amet gravida quam, vel tempor nibh. Sed viverra fringilla lacinia. Nam turpis arcu, auctor vitae ante a, tempor tempus arcu. Donec pharetra auctor massa, nec molestie sem pretium non. Nulla dignissim justo enim, at ultrices ligula tempor ac. In in turpis sem.";
            string inputFilePath = CreateTestFile("app.log", originalContent); // .log is accepted
            string compressedFilePath = Path.Combine(_testDirectory, "app.huff");
            string decompressedFilePath = Path.Combine(_testDirectory, "app.decompressed");

            // Act
            CompressionResult compressionResult = _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert
            Assert.True(compressionResult.IsSuccess);
            Assert.True(File.Exists(compressedFilePath));
            Assert.True(compressionResult.OriginalSize > compressionResult.CompressedSize);

            // Act - Decompress
            DecompressionResult decompressionResult = _compressor.DecompressFile(compressedFilePath, decompressedFilePath);

            // Assert - Decompression
            Assert.True(decompressionResult.IsSuccess);
            Assert.True(File.Exists(decompressedFilePath));
            Assert.Equal(compressionResult.OriginalSize, decompressionResult.DecompressedSize);
            Assert.Equal(originalContent, File.ReadAllText(decompressedFilePath, Encoding.UTF8));
        }

        [Fact]
        public void Compress_WithDefaultOutputDirectory_ShouldSaveCorrectly()
        {
            // Arrange
            string customOutputDir = Path.Combine(_testDirectory, "CustomOutput");
            Directory.CreateDirectory(customOutputDir);
            HuffmanFileCompressor.CompressionSettings.DefaultOutputDirectory = customOutputDir;

            string originalContent = "Content for custom output.";
            string inputFilePath = CreateTestFile("custom_output_test.txt", originalContent);
            string expectedCompressedPath = Path.Combine(customOutputDir, "custom_output_test.huff");

            // Act
            CompressionResult result = _compressor.CompressFile(inputFilePath); // No output path specified

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedCompressedPath, result.OutputFilePath);
            Assert.True(File.Exists(expectedCompressedPath));
        }

        [Fact]
        public void Decompress_WithDefaultOutputDirectory_ShouldSaveCorrectly()
        {
            // Arrange - First compress a file to get a .huff file
            string originalContent = "Content to be decompressed in custom output.";
            string inputFilePath = CreateTestFile("decompress_custom_output.txt", originalContent);
            string compressedFilePath = Path.Combine(_testDirectory, "decompress_custom_output.huff");
            _compressor.CompressFile(inputFilePath, compressedFilePath);

            string customOutputDir = Path.Combine(_testDirectory, "CustomDecompressOutput");
            Directory.CreateDirectory(customOutputDir);
            HuffmanFileCompressor.CompressionSettings.DefaultOutputDirectory = customOutputDir;

            string expectedDecompressedPath = Path.Combine(customOutputDir, "decompress_custom_output.decompressed");

            // Act
            DecompressionResult result = _compressor.DecompressFile(compressedFilePath); // No output path specified

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedDecompressedPath, result.OutputFilePath);
            Assert.True(File.Exists(expectedDecompressedPath));
            Assert.Equal(originalContent, File.ReadAllText(expectedDecompressedPath, Encoding.UTF8));
        }

        [Fact]
        public void Events_CompressionProgressChanged_ShouldFire()
        {
            // Arrange
            string originalContent = new string('A', 1000) + new string('B', 500) + new string('C', 2000); // Larger content for progress
            string inputFilePath = CreateTestFile("progress_test.txt", originalContent);
            string compressedFilePath = Path.Combine(_testDirectory, "progress_test.huff");

            int lastProgress = 0;
            bool progressEventFired = false;
            _compressor.CompressionProgressChanged += (sender, e) =>
            {
                progressEventFired = true;
                Assert.True(e.ProgressPercentage >= lastProgress);
                lastProgress = e.ProgressPercentage;
            };

            // Act
            _compressor.CompressFile(inputFilePath, compressedFilePath);

            // Assert
            Assert.True(progressEventFired); // Ensure at least one progress event fired
            Assert.True(lastProgress >= 99); // Should reach near 100%
        }

    }
}
