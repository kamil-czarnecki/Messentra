using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Messentra.UnitTests;

public abstract class InMemoryDbTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly MessentraDbContext DbContext;
    protected readonly TestDbContextFactory DbContextFactory;

    protected InMemoryDbTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create DbContext options
        var options = new DbContextOptionsBuilder<MessentraDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the context with the in-memory database
        DbContext = new MessentraDbContext(options);
        DbContextFactory = new TestDbContextFactory(DbContext);

        // Ensure the database schema is created
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}

