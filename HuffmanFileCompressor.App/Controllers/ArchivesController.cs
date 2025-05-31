using HuffmanFileCompressor.App.Model;
using HuffmanFileCompressor.App.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Crucial for injecting ILogger

namespace HuffmanFileCompressor.App.Controllers
{
    [Route("api/archives")]
    [ApiController]
    public class ArchivesController : ControllerBase
    {
        private readonly IHuffmanFileCompressor _huffmanFileCompressor;
        private readonly ApplicationDbContext _context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
        private readonly string _decompressionPath = Path.Combine(Directory.GetCurrentDirectory(), "DecompressedFiles");
        // private static readonly ILog Logger = LogManager.GetLogger(typeof(ArchivesController)); // REMOVE THIS LINE
        private readonly ILogger<ArchivesController> _logger; // ADD THIS LINE

        // Constructor to inject both DbContext and ILogger
        public ArchivesController(ApplicationDbContext context, ILogger<ArchivesController> logger)
        {
            _context = context;
            _logger = logger; // Assign the injected logger
            _huffmanFileCompressor = new HuffmanFileCompressor();

            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
            if (!Directory.Exists(_decompressionPath))
                Directory.CreateDirectory(_decompressionPath);

            _logger.LogInformation("ArchivesController created."); // Use the injected logger
        }


        //Compress file
        [HttpPost("compress")]
        public async Task<IActionResult> Compress(IFormFile file, string archiveName)
        {
            _logger.LogInformation("Compress endpoint called for file: {FileName}", file.FileName);

            if (file.Length == 0)
            {
                _logger.LogWarning("No file uploaded for compression.");
                return BadRequest("No file uploaded.");
            }
            string inputFilePath = await SaveFile(file);
            string extension = Path.GetExtension(file.FileName);
            Guid guid = Guid.NewGuid();
            string outputFileName = $"{guid}.huff";
            string outputPath = Path.Combine(_storagePath, outputFileName);

            try
            {
                _logger.LogInformation("Attempting to compress file from {InputPath} to {OutputPath}", inputFilePath, outputPath);
                CompressionResult result = _huffmanFileCompressor.CompressFile(inputFilePath, outputPath);

                Archives archives = new Archives
                {
                    Name = archiveName,
                    Path = outputPath,
                    CompressedSize = (int)result.CompressedSize,
                    OriginalSize = (int)result.OriginalSize,
                    Extension = extension,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                };
                _context.Archives.Add(archives);
                await _context.SaveChangesAsync();
                DeleteFile(inputFilePath); // Assuming this deletes the temporary compressed file after DB save
                _logger.LogInformation("File '{FileName}' compressed and archive record saved. Compressed size: {CompressedSize} bytes", file.FileName, result.CompressedSize);
                return Ok(archives);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during file compression for {FileName}", file.FileName);
                // Console.WriteLine(e); // Avoid Console.WriteLine in production code
                throw; // Re-throw the exception after logging
            }
        }

        [HttpPost("decompress")]
        public async Task<IActionResult> Decompress(int id)
        {
            _logger.LogInformation("Decompress endpoint called for Archive ID: {ArchiveId}", id);
            bool isIdExist = await _context.Archives.AnyAsync(a => a.Id == id);

            if (!isIdExist)
            {
                _logger.LogError("Decompression request failed: Archive with ID {ArchiveId} not found.", id);
                return BadRequest($"ID {id} not found."); // Return specific BadRequest message
            }

            try
            {
                var query = await _context.Archives.FindAsync(id);
                string outputFileName = Path.Combine(_decompressionPath, $"{query!.Name}{query.Extension}");
                _logger.LogInformation("Attempting to decompress file from {InputPath} to {OutputPath}", query.Path, outputFileName);
                DecompressionResult result = _huffmanFileCompressor.DecompressFile(query.Path, outputFileName);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Decompression endpoint returned error: {Error}", result.ErrorMessage);
                    _logger.LogDebug($"File path :{query.Path}");
                    return BadRequest(result.ErrorMessage);
                }
                _logger.LogInformation("File decompressed successfully for Archive ID: {ArchiveId}", id);
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during file decompression for Archive ID: {ArchiveId}", id);
                // Console.WriteLine(e);
                throw;
            }
        }

        // GET: api/archives
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Archives>>> GetArchives()
        {
            _logger.LogInformation("GetArchives endpoint called.");
            return await _context.Archives.ToListAsync();
        }

        // GET: api/archives/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Archives>> GetArchive(int id)
        {
            _logger.LogInformation("GetArchive endpoint called for ID: {ArchiveId}", id);
            var archive = await _context.Archives.FindAsync(id);

            if (archive == null)
            {
                _logger.LogWarning("Archive with ID {ArchiveId} not found.", id);
                return NotFound($"Archive with ID {id} not found.");
            }

            _logger.LogInformation("Archive with ID {ArchiveId} found.", id);
            return archive;
        }

        // POST: api/archives
        [HttpPost]
        public async Task<ActionResult<Archives>> PostArchive(Archives archive)
        {
            _logger.LogInformation("PostArchive endpoint called for archive: {ArchiveName}", archive.Name);
            archive.Created = DateTime.UtcNow;
            archive.Modified = DateTime.UtcNow;

            _context.Archives.Add(archive);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New archive created with ID: {ArchiveId}", archive.Id);
            return CreatedAtAction(nameof(GetArchive), new { id = archive.Id }, archive);
        }

        // PUT: api/archives/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutArchive(int id, Archives archive)
        {
            _logger.LogInformation("PutArchive endpoint called for ID: {ArchiveId}", id);

            if (id != archive.Id)
            {
                _logger.LogWarning("Archive ID mismatch during PUT operation. Route ID: {RouteId}, Body ID: {BodyId}", id, archive.Id);
                return BadRequest("Archive ID mismatch.");
            }

            _context.Entry(archive).State = EntityState.Modified;
            archive.Modified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archive with ID {ArchiveId} updated successfully.", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ArchiveExists(id))
                {
                    _logger.LogWarning(ex, "Archive with ID {ArchiveId} not found during update concurrency check.", id);
                    return NotFound($"Archive with ID {id} not found.");
                }
                else
                {
                    _logger.LogError(ex, "Concurrency exception during update of Archive ID: {ArchiveId}", id);
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/archives/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArchive(int id)
        {
            _logger.LogInformation("DeleteArchive endpoint called for ID: {ArchiveId}", id);
            var archive = await _context.Archives.FindAsync(id);
            if (archive == null)
            {
                _logger.LogWarning("Archive with ID {ArchiveId} not found for deletion.", id);
                return NotFound($"Archive with ID {id} not found.");
            }

            _context.Archives.Remove(archive);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Archive with ID {ArchiveId} deleted successfully.", id);
            return NoContent();
        }

        private bool ArchiveExists(int id)
        {
            return _context.Archives.Any(e => e.Id == id);
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, file.FileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _logger.LogDebug("File '{FileName}' saved to '{FilePath}'", file.FileName, filePath);
                return filePath;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to save file: {FileName}", file.FileName);
                throw new Exception($"Failed to save file: {e.Message}");
            }
        }

        private void DeleteFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogDebug("File '{FilePath}' deleted.", filePath);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete file '{FilePath}', but it did not exist.", filePath);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete file: {FileName}", fileName);
                throw new Exception($"Failed to delete file: {e.Message}");
            }
        }
    }
}