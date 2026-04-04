using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Messentra.UnitTests;

public sealed class TestDbContextFactory : IDbContextFactory<MessentraDbContext>
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory(MessentraDbContext dbContext)
    {
        _connection = (SqliteConnection)dbContext.Database.GetDbConnection();
    }

    public MessentraDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MessentraDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new MessentraDbContext(options);
    }

    public Task<MessentraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}


