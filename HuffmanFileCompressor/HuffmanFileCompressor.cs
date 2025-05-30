// HuffmanFileCompressor/HuffmanFileCompressor.cs
using System.Text;
using log4net;
using log4net.Config;
using log4net.Repository;
using System.Reflection;

namespace HuffmanFileCompressor
{
    /// <summary>
    /// Provides methods for compressing and decompressing files using Huffman coding.
    /// </summary>
    public class HuffmanFileCompressor:IHuffmanFileCompressor
    {
        // Event handlers for progress and completion feedback
        public event EventHandler<CompressionProgressEventArgs>? CompressionProgressChanged;
        public event EventHandler<CompressionCompletedEventArgs>? CompressionCompleted;
        public event EventHandler<DecompressionCompletedEventArgs>? DecompressionCompleted;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;
        private static readonly ILog Logger;
        static HuffmanFileCompressor()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            Logger = LogManager.GetLogger(typeof(HuffmanFileCompressor));
        }
        /// <summary>
        /// Represents the configuration settings for the Huffman compressor.
        /// </summary>
        public static class CompressionSettings
        {
            /// <summary>
            /// Gets or sets a list of file extensions that are accepted for compression.
            /// If null or empty, all file types are accepted.
            /// Example: new string[] { ".txt", ".pdf", ".docx" }
            /// </summary>
            public static string[]? AcceptedFileExtensions { get; set; }

            /// <summary>
            /// Gets or sets the default directory where compressed/decompressed files will be saved.
            /// If null or empty, the same directory as the input file will be used.
            /// </summary>
            public static string? DefaultOutputDirectory { get; set; }
        }

        /// <summary>
        /// Compresses a file using Huffman coding.
        /// </summary>
        /// <param name="inputFilePath">The path to the file to compress.</param>
        /// <param name="outputFilePath">The path where the compressed file will be saved. If null or empty, a default path will be generated.</param>
        /// <returns>A CompressionResult object indicating the outcome of the operation.</returns>
        public CompressionResult CompressFile(string inputFilePath, string? outputFilePath = null)
        {
            try
            {
                // Validate input file path
                Logger.Info($"Starting compression for file: {inputFilePath}");
                if (string.IsNullOrWhiteSpace(inputFilePath) || !File.Exists(inputFilePath))
                {
                    throw new ArgumentException("Input file path is invalid or file does not exist.", nameof(inputFilePath));
                }

                // Check if file extension is accepted
                if (CompressionSettings.AcceptedFileExtensions != null && CompressionSettings.AcceptedFileExtensions.Length > 0)
                {
                    string fileExtension = Path.GetExtension(inputFilePath).ToLowerInvariant();
                    if (!CompressionSettings.AcceptedFileExtensions.Contains(fileExtension))
                    {
                        throw new NotSupportedException($"File type '{fileExtension}' is not accepted for compression.");
                    }
                }

                // Determine output file path
                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputDir = string.IsNullOrWhiteSpace(CompressionSettings.DefaultOutputDirectory) ?
                                       Path.GetDirectoryName(inputFilePath)! :
                                       CompressionSettings.DefaultOutputDirectory;

                    Directory.CreateDirectory(outputDir); // Ensure output directory exists
                    outputFilePath = Path.Combine(outputDir, $"{fileNameWithoutExtension}.huff"); // .huff extension for Huffman compressed files
                }

                byte[] inputBytes = File.ReadAllBytes(inputFilePath);
                long originalSize = inputBytes.Length;

                // Step 1: Build Huffman Tree and Codes
                HuffmanTree huffmanTree = new HuffmanTree();
                huffmanTree.Build(inputBytes);

                // Step 2: Encode the input bytes
                byte[] encodedBytes = huffmanTree.Encode(inputBytes, (progress) =>
                {
                    OnCompressionProgressChanged(new CompressionProgressEventArgs(progress));
                });

                // Step 3: Write the encoded data and tree structure to the output file
                using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(outputStream))
                {
                    // Write the frequency table (needed for decompression)
                    writer.Write(huffmanTree.GetFrequencyTableSize());
                    foreach (var entry in huffmanTree.GetFrequencyTable())
                    {
                        writer.Write(entry.Key); // Byte value
                        writer.Write(entry.Value); // Frequency
                    }

                    // Write the length of the original data (useful for padding and verification)
                    writer.Write(originalSize);

                    // Write the encoded data
                    writer.Write(encodedBytes.Length); // Length of the encoded byte array
                    writer.Write(encodedBytes);
                }

                long compressedSize = new FileInfo(outputFilePath).Length;
                double compressionRatio = (double)compressedSize / originalSize;

                CompressionResult result = new CompressionResult(true, originalSize, compressedSize, compressionRatio, outputFilePath);
                OnCompressionCompleted(new CompressionCompletedEventArgs(result));
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs(ex.Message));
                return new CompressionResult(false, 0, 0, 0, null, ex.Message);
            }
        }

        /// <summary>
        /// Decompresses a Huffman-coded file.
        /// </summary>
        /// <param name="inputFilePath">The path to the compressed file.</param>
        /// <param name="outputFilePath">The path where the decompressed file will be saved. If null or empty, a default path will be generated.</param>
        /// <returns>A DecompressionResult object indicating the outcome of the operation.</returns>
        public DecompressionResult DecompressFile(string inputFilePath, string? outputFilePath = null)
        {
            try
            {
                // Validate input file path
                if (string.IsNullOrWhiteSpace(inputFilePath) || !File.Exists(inputFilePath))
                {
                    throw new ArgumentException("Input file path is invalid or file does not exist.", nameof(inputFilePath));
                }

                // Determine output file path
                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputDir = string.IsNullOrWhiteSpace(CompressionSettings.DefaultOutputDirectory) ?
                                       Path.GetDirectoryName(inputFilePath)! :
                                       CompressionSettings.DefaultOutputDirectory;

                    Directory.CreateDirectory(outputDir); // Ensure output directory exists
                    outputFilePath = Path.Combine(outputDir, $"{fileNameWithoutExtension}.decompressed"); // Add .decompressed extension
                }

                byte[] decompressedBytes;
                long originalDataLength;

                using (FileStream inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(inputStream))
                {
                    // Read the frequency table
                    int frequencyTableSize = reader.ReadInt32();
                    Dictionary<byte, long> frequencyTable = new Dictionary<byte, long>();
                    for (int i = 0; i < frequencyTableSize; i++)
                    {
                        byte character = reader.ReadByte();
                        long frequency = reader.ReadInt64();
                        frequencyTable.Add(character, frequency);
                    }

                    // Rebuild Huffman Tree from frequency table
                    HuffmanTree huffmanTree = new HuffmanTree();
                    huffmanTree.BuildFromFrequencyTable(frequencyTable);

                    // Read the original data length
                    originalDataLength = reader.ReadInt64();

                    // Read the encoded data
                    int encodedLength = reader.ReadInt32();
                    byte[] encodedBytes = reader.ReadBytes(encodedLength);

                    // Decode the bytes
                    decompressedBytes = huffmanTree.Decode(encodedBytes, originalDataLength, (progress) =>
                    {
                        // No direct progress for decode in this basic implementation, but could be added
                    });
                }

                // Write the decompressed bytes to the output file
                File.WriteAllBytes(outputFilePath, decompressedBytes);

                DecompressionResult result = new DecompressionResult(true, originalDataLength, decompressedBytes.Length, outputFilePath);
                OnDecompressionCompleted(new DecompressionCompletedEventArgs(result));
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs(ex.Message));
                return new DecompressionResult(false, 0, 0, null, ex.Message);
            }
        }

        // Event raisers
        protected virtual void OnCompressionProgressChanged(CompressionProgressEventArgs e)
        {
            CompressionProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnCompressionCompleted(CompressionCompletedEventArgs e)
        {
            CompressionCompleted?.Invoke(this, e);
        }

        protected virtual void OnDecompressionCompleted(DecompressionCompletedEventArgs e)
        {
            DecompressionCompleted?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Represents the result of a compression operation.
    /// </summary>
    public class CompressionResult
    {
        public bool IsSuccess { get; }
        public long OriginalSize { get; }
        public long CompressedSize { get; }
        
        public double CompressionRatio { get; } // CompressedSize / OriginalSize
        public string? OutputFilePath { get; }
        public string? ErrorMessage { get; }

        public CompressionResult(bool isSuccess, long originalSize, long compressedSize, double compressionRatio, string? outputFilePath, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            OriginalSize = originalSize;
            CompressedSize = compressedSize;
            CompressionRatio = compressionRatio;
            OutputFilePath = outputFilePath;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Represents the result of a decompression operation.
    /// </summary>
    public class DecompressionResult
    {
        public bool IsSuccess { get; }
        public long OriginalDataLength { get; } // Length of the data before compression
        public long DecompressedSize { get; } // Length of the data after decompression
        public string? OutputFilePath { get; }
        public string? ErrorMessage { get; }

        public DecompressionResult(bool isSuccess, long originalDataLength, long decompressedSize, string? outputFilePath, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            OriginalDataLength = originalDataLength;
            DecompressedSize = decompressedSize;
            OutputFilePath = outputFilePath;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Event arguments for compression progress updates.
    /// </summary>
    public class CompressionProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }

        public CompressionProgressEventArgs(int progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }
    }

    /// <summary>
    /// Event arguments for compression completion.
    /// </summary>
    public class CompressionCompletedEventArgs : EventArgs
    {
        public CompressionResult Result { get; }

        public CompressionCompletedEventArgs(CompressionResult result)
        {
            Result = result;
        }
    }

    /// <summary>
    /// Event arguments for decompression completion.
    /// </summary>
    public class DecompressionCompletedEventArgs : EventArgs
    {
        public DecompressionResult Result { get; }

        public DecompressionCompletedEventArgs(DecompressionResult result)
        {
            Result = result;
        }
    }

    /// <summary>
    /// Event arguments for error occurrences.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public ErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Represents a node in the Huffman tree.
    /// </summary>
    internal class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte? Character { get; set; } // Null for internal nodes
        public long Frequency { get; set; }
        public HuffmanNode? Left { get; set; }
        public HuffmanNode? Right { get; set; }

        public bool IsLeaf => Left == null && Right == null;

        public HuffmanNode(byte? character, long frequency)
        {
            Character = character;
            Frequency = frequency;
        }

        public HuffmanNode(HuffmanNode left, HuffmanNode right)
        {
            Character = null; // Internal node
            Frequency = left.Frequency + right.Frequency;
            Left = left;
            Right = right;
        }

        public int CompareTo(HuffmanNode? other)
        {
            if (other == null) return 1;
            return Frequency.CompareTo(other.Frequency);
        }
    }

    /// <summary>
    /// Implements the Huffman coding algorithm.
    /// </summary>
    internal class HuffmanTree
    {
        private HuffmanNode? _root;
        private Dictionary<byte, string>? _huffmanCodes;
        private Dictionary<byte, long>? _frequencyTable;

        /// <summary>
        /// Builds the Huffman tree and generates Huffman codes from the input bytes.
        /// </summary>
        /// <param name="inputBytes">The bytes to analyze for frequency.</param>
        public void Build(byte[] inputBytes)
        {
            // Calculate character frequencies
            _frequencyTable = new Dictionary<byte, long>();
            foreach (byte b in inputBytes)
            {
                if (_frequencyTable.ContainsKey(b))
                {
                    _frequencyTable[b]++;
                }
                else
                {
                    _frequencyTable[b] = 1;
                }
            }

            // Create leaf nodes for each character
            PriorityQueue<HuffmanNode, long> nodes = new PriorityQueue<HuffmanNode, long>();
            foreach (var entry in _frequencyTable)
            {
                nodes.Enqueue(new HuffmanNode(entry.Key, entry.Value), entry.Value);
            }

            // Build the Huffman tree
            while (nodes.Count > 1)
            {
                HuffmanNode left = nodes.Dequeue();
                HuffmanNode right = nodes.Dequeue();
                HuffmanNode parent = new HuffmanNode(left, right);
                nodes.Enqueue(parent, parent.Frequency);
            }

            _root = nodes.Dequeue();

            // Generate Huffman codes
            _huffmanCodes = new Dictionary<byte, string>();
            GenerateCodes(_root, "", _huffmanCodes);
        }

        /// <summary>
        /// Rebuilds the Huffman tree from a given frequency table (used for decompression).
        /// </summary>
        /// <param name="frequencyTable">The frequency table to reconstruct the tree from.</param>
        public void BuildFromFrequencyTable(Dictionary<byte, long> frequencyTable)
        {
            _frequencyTable = frequencyTable; // Store the frequency table for potential later use

            PriorityQueue<HuffmanNode, long> nodes = new PriorityQueue<HuffmanNode, long>();
            foreach (var entry in frequencyTable)
            {
                nodes.Enqueue(new HuffmanNode(entry.Key, entry.Value), entry.Value);
            }

            while (nodes.Count > 1)
            {
                HuffmanNode left = nodes.Dequeue();
                HuffmanNode right = nodes.Dequeue();
                HuffmanNode parent = new HuffmanNode(left, right);
                nodes.Enqueue(parent, parent.Frequency);
            }

            _root = nodes.Dequeue();
        }

        /// <summary>
        /// Recursively generates Huffman codes for each character.
        /// </summary>
        /// <param name="node">The current node in the tree.</param>
        /// <param name="currentCode">The binary code accumulated so far.</param>
        /// <param name="codes">The dictionary to store the character-code mappings.</param>
        private void GenerateCodes(HuffmanNode node, string currentCode, Dictionary<byte, string> codes)
        {
            if (node.IsLeaf)
            {
                if (node.Character.HasValue)
                {
                    codes[node.Character.Value] = currentCode;
                }
            }
            else
            {
                if (node.Left != null)
                {
                    GenerateCodes(node.Left, currentCode + "0", codes);
                }
                if (node.Right != null)
                {
                    GenerateCodes(node.Right, currentCode + "1", codes);
                }
            }
        }

        /// <summary>
        /// Encodes the input bytes using the generated Huffman codes.
        /// </summary>
        /// <param name="inputBytes">The bytes to encode.</param>
        /// <param name="progressCallback">Callback for reporting progress.</param>
        /// <returns>The encoded byte array.</returns>
        public byte[] Encode(byte[] inputBytes, Action<int>? progressCallback = null)
        {
            if (_huffmanCodes == null || _root == null)
            {
                throw new InvalidOperationException("Huffman tree and codes must be built before encoding.");
            }

            using (MemoryStream ms = new MemoryStream())
            using (BitWriter writer = new BitWriter(ms))
            {
                long totalBytes = inputBytes.Length;
                long processedBytes = 0;
                int lastReportedProgress = 0;

                foreach (byte b in inputBytes)
                {
                    if (_huffmanCodes.TryGetValue(b, out string? code))
                    {
                        foreach (char bitChar in code)
                        {
                            writer.WriteBit(bitChar == '1');
                        }
                    }
                    processedBytes++;

                    int currentProgress = (int)((double)processedBytes / totalBytes * 100);
                    if (currentProgress > lastReportedProgress)
                    {
                        progressCallback?.Invoke(currentProgress);
                        lastReportedProgress = currentProgress;
                    }
                }
                writer.Flush(); // Ensure all buffered bits are written
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decodes the Huffman-encoded bytes.
        /// </summary>
        /// <param name="encodedBytes">The encoded byte array.</param>
        /// <param name="originalDataLength">The expected length of the original data.</param>
        /// <param name="progressCallback">Callback for reporting progress (currently not fully implemented for decode).</param>
        /// <returns>The decompressed byte array.</returns>
        public byte[] Decode(byte[] encodedBytes, long originalDataLength, Action<int>? progressCallback = null)
        {
            if (_root == null)
            {
                throw new InvalidOperationException("Huffman tree must be built before decoding.");
            }

            using (MemoryStream ms = new MemoryStream(encodedBytes))
            using (BitReader reader = new BitReader(ms))
            using (MemoryStream outputMs = new MemoryStream())
            {
                HuffmanNode? currentNode = _root;
                long decompressedBytesCount = 0;

                while (decompressedBytesCount < originalDataLength)
                {
                    if (currentNode == null)
                    {
                        throw new InvalidOperationException("Huffman tree root is null during decoding.");
                    }

                    if (currentNode.IsLeaf)
                    {
                        if (currentNode.Character.HasValue)
                        {
                            outputMs.WriteByte(currentNode.Character.Value);
                            decompressedBytesCount++;
                            currentNode = _root; // Reset to root for the next character
                        }
                        else
                        {
                            throw new InvalidDataException("Encountered a leaf node without a character during decoding.");
                        }
                    }
                    else
                    {
                        if (!reader.CanReadBit())
                        {
                            // This can happen if the encoded data ends exactly on a byte boundary
                            // and the last character is a leaf node.
                            // If originalDataLength is reached, we are done.
                            if (decompressedBytesCount == originalDataLength) break;
                            throw new EndOfStreamException("Unexpected end of stream during Huffman decoding.");
                        }

                        bool bit = reader.ReadBit();
                        if (bit)
                        {
                            currentNode = currentNode.Right;
                        }
                        else
                        {
                            currentNode = currentNode.Left;
                        }
                    }
                }
                return outputMs.ToArray();
            }
        }

        /// <summary>
        /// Gets the frequency table used to build the Huffman tree.
        /// </summary>
        /// <returns>A dictionary mapping bytes to their frequencies.</returns>
        public Dictionary<byte, long> GetFrequencyTable()
        {
            if (_frequencyTable == null)
            {
                throw new InvalidOperationException("Frequency table has not been built.");
            }
            return _frequencyTable;
        }

        /// <summary>
        /// Gets the size of the frequency table.
        /// </summary>
        /// <returns>The number of entries in the frequency table.</returns>
        public int GetFrequencyTableSize()
        {
            return _frequencyTable?.Count ?? 0;
        }
    }

    /// <summary>
    /// Helper class for writing individual bits to a stream.
    /// </summary>
    internal class BitWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private byte _currentByte;
        private int _bitsWritten;

        public BitWriter(Stream baseStream)
        {
            _baseStream = baseStream;
            _currentByte = 0;
            _bitsWritten = 0;
        }

        /// <summary>
        /// Writes a single bit to the stream.
        /// </summary>
        /// <param name="bit">True for '1', False for '0'.</param>
        public void WriteBit(bool bit)
        {
            if (bit)
            {
                _currentByte |= (byte)(1 << (7 - _bitsWritten)); // Set the bit
            }
            // If bit is false, it's already 0, so no change needed

            _bitsWritten++;

            if (_bitsWritten == 8)
            {
                _baseStream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitsWritten = 0;
            }
        }

        /// <summary>
        /// Flushes any remaining buffered bits to the stream, padding with zeros if necessary.
        /// </summary>
        public void Flush()
        {
            if (_bitsWritten > 0)
            {
                _baseStream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitsWritten = 0;
            }
        }

        public void Dispose()
        {
            Flush(); // Ensure all bits are written when disposing
        }
    }

    /// <summary>
    /// Helper class for reading individual bits from a stream.
    /// </summary>
    internal class BitReader : IDisposable
    {
        private readonly Stream _baseStream;
        private byte _currentByte;
        private int _bitsRead;
        private bool _endOfStream;

        public BitReader(Stream baseStream)
        {
            _baseStream = baseStream;
            _currentByte = 0;
            _bitsRead = 8; // Initialize to 8 so the first ReadBit triggers a byte read
            _endOfStream = false;
        }

        /// <summary>
        /// Reads a single bit from the stream.
        /// </summary>
        /// <returns>True for '1', False for '0'.</returns>
        public bool ReadBit()
        {
            if (_bitsRead == 8)
            {
                int byteRead = _baseStream.ReadByte();
                if (byteRead == -1)
                {
                    _endOfStream = true;
                    throw new EndOfStreamException("Attempted to read beyond the end of the bit stream.");
                }
                _currentByte = (byte)byteRead;
                _bitsRead = 0;
            }

            bool bit = ((_currentByte >> (7 - _bitsRead)) & 1) == 1;
            _bitsRead++;
            return bit;
        }

        /// <summary>
        /// Checks if there are more bits to read in the stream.
        /// </summary>
        /// <returns>True if more bits can be read, false otherwise.</returns>
        public bool CanReadBit()
        {
            if (_endOfStream) return false;

            // If we have bits in the current byte, we can read.
            if (_bitsRead < 8) return true;

            // If current byte is exhausted, check if there's another byte available.
            return _baseStream.Position < _baseStream.Length;
        }

        public void Dispose()
        {
            // No specific flush needed for reader
        }
    }
}
