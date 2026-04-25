using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Messentra.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
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
        ConnectionStringProtection.Initialize(new EphemeralDataProtectionProvider());

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MessentraDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new MessentraDbContext(options);
        DbContextFactory = new TestDbContextFactory(DbContext);

        DbContext.Database.EnsureCreated();
    }

    protected async Task<Connection> SeedConnectionAsync(string? name = null)
    {
        var connection = new Connection
        {
            Name = name ?? $"Test Connection {Guid.NewGuid():N}",
            ConnectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/")
        };
        await DbContext.Set<Connection>().AddAsync(connection);
        await DbContext.SaveChangesAsync();
        return connection;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}

