using HuffmanFileCompressor.App.Model;
using HuffmanFileCompressor.App.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HuffmanFileCompressor.App.Controllers
{
    [Route("api/archives")] // Specifies the base route for this controller
    [ApiController] // Indicates that this class is an API controller
    public class ArchivesController : ControllerBase
    {
        private readonly IHuffmanFileCompressor _huffmanFileCompressor;
        private readonly ApplicationDbContext _context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
        private readonly ILogger<ArchivesController> _logger;
        // Constructor to inject the ApplicationDbContext
        public ArchivesController(ApplicationDbContext context,  ILogger<ArchivesController> logger)
        {
            _logger = logger;
            _context = context;
            _huffmanFileCompressor = new HuffmanFileCompressor();
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }
        
        
        //Compress file
        [HttpPost("compress")]
        public async Task<IActionResult> Compress(IFormFile file, string archiveName)
        {
            _logger.LogInformation("archiveName : {archiveName}", archiveName);
            if (file.Length == 0)
                return BadRequest("No file uploaded.");
            string inputFilePath = await SaveFile(file);
            Guid guid = Guid.NewGuid();
            string outputFileName = $"{guid}.huff";
            string outputPath = Path.Combine(_storagePath, outputFileName);

            try
            {
                CompressionResult result = _huffmanFileCompressor.CompressFile(inputFilePath, outputPath);
                
                Archives archives = new Archives
                {
                    Name = archiveName,
                    Path = outputPath,
                    CompressedSize = (int)result.CompressedSize,
                    OriginalSize = (int)result.OriginalSize,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                };
                _context.Archives.Add(archives);
                await _context.SaveChangesAsync();
                DeleteFile(outputPath);
                return Ok(archives);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            
            
            return Ok(new { message = "File uploaded successfully.", inputFilePath });
            
            
        }
        // GET: api/archives
        // Retrieves all archives from the database
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Archives>>> GetArchives()
        {
            // Return all archives as a list
            return await _context.Archives.ToListAsync();
        }

        // GET: api/archives/{id}
        // Retrieves a single archive by its ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Archives>> GetArchive(int id)
        {

            // Find the archive by ID
            var archive = await _context.Archives.FindAsync(id);

            // If no archive is found with the given ID, return 404 Not Found
            if (archive == null)
            {
                return NotFound($"Archive with ID {id} not found.");
            }

            // Return the found archive
            return archive;
        }

        // POST: api/archives
        // Creates a new archive entry
        [HttpPost]
        public async Task<ActionResult<Archives>> PostArchive(Archives archive)
        {
            // Set creation and modification timestamps
            archive.Created = DateTime.UtcNow;
            archive.Modified = DateTime.UtcNow;

            // Add the new archive to the DbSet
            _context.Archives.Add(archive);
            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return a 201 Created response with the newly created archive and its URI
            return CreatedAtAction(nameof(GetArchive), new { id = archive.Id }, archive);
        }

        // PUT: api/archives/{id}
        // Updates an existing archive entry
        [HttpPut("{id}")]
        public async Task<IActionResult> PutArchive(int id, Archives archive)
        {
            // Check if the ID in the route matches the ID in the archive object
            if (id != archive.Id)
            {
                return BadRequest("Archive ID mismatch.");
            }

            // Mark the archive entity as modified
            _context.Entry(archive).State = EntityState.Modified;

            // Update the Modified timestamp
            archive.Modified = DateTime.UtcNow;

            try
            {
                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If a concurrency exception occurs, check if the archive exists
                if (!ArchiveExists(id))
                {
                    return NotFound($"Archive with ID {id} not found.");
                }
                else
                {
                    // Re-throw the exception if it's not a "not found" issue
                    throw;
                }
            }

            // Return 204 No Content on successful update
            return NoContent();
        }

        // DELETE: api/archives/{id}
        // Deletes an archive entry by its ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArchive(int id)
        {
            _logger.LogInformation($"The file with id : {id} successfully deleted.");
            // Find the archive to delete
            var archive = await _context.Archives.FindAsync(id);
            if (archive == null)
            {
                return NotFound($"Archive with ID {id} not found.");
            }

            // Remove the archive from the DbSet
            _context.Archives.Remove(archive);
            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return 204 No Content on successful deletion
            return NoContent();
        }

        // Helper method to check if an archive exists
        private bool ArchiveExists(int id) { return _context.Archives.Any(e => e.Id == id);}

        private async Task<string> SaveFile(IFormFile file)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, file.FileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                return filePath;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private bool DeleteFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to delete file: {e.Message}");
            }
        }
    }
}