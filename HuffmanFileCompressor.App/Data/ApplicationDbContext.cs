using Microsoft.EntityFrameworkCore;
using HuffmanFileCompressor.App.Model;
namespace HuffmanFileCompressor.App.Data;

public class ApplicationDbContext:DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
        base(options)
    {
        
    }
    
    public DbSet<Archives> Archives { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Archives>().HasData(
            new Archives
            {
                Id = 1,
                Name = "Archive 1 file",
                Path = "Archive 1 file",
                OriginalSize = 100,
                CompressedSize = 100,
                Created = new DateTime(2025, 5, 27, 15, 10, 45),
                Modified = new DateTime(2025, 5, 27, 15, 10, 45)
            }
        );
    }
}