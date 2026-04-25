using System.Security.Cryptography;
using System.Text.Json;
using Messentra.Domain;
using Messentra.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Connections;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("Connections");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(100).UseCollation("NOCASE").IsRequired();
        builder.Property(x => x.ConnectionConfig)
            .IsRequired()
            .HasConversion(
                v => ConnectionStringProtection.Protect(
                    JsonSerializer.Serialize(v, JsonSerializerOptions.Default)),
                v => Deserialize(v));

        builder
            .HasIndex(x => x.Name)
            .IsUnique();
    }

    private static ConnectionConfig Deserialize(string v)
    {
        try
        {
            try
            {
                return JsonSerializer.Deserialize<ConnectionConfig>(
                    ConnectionStringProtection.Unprotect(v), JsonSerializerOptions.Default)!;
            }
            catch (CryptographicException)
            {
                // Plain-text fallback for rows not yet migrated
                return JsonSerializer.Deserialize<ConnectionConfig>(v, JsonSerializerOptions.Default)!;
            }
        }
        catch
        {
            // Key was lost or data is irrecoverably corrupted
            return ConnectionConfig.CreateCorrupted();
        }
    }
}