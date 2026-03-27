using Microsoft.EntityFrameworkCore;

namespace Messentra.Infrastructure.Database;

public class MessentraDbContext : DbContext
{
    public MessentraDbContext()
    {
    }
    
    public MessentraDbContext(DbContextOptions<MessentraDbContext> options) : base(options)
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var dbDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra");

        Directory.CreateDirectory(dbDirectory);

        var dbPath = Path.Combine(dbDirectory, "Messentra.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessentraDbContext).Assembly);
    }
}
