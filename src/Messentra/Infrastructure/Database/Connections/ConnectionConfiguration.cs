using System.Text.Json;
using Messentra.Domain;
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
                v => JsonSerializer.Serialize(v, System.Text.Json.JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<ConnectionConfig>(v, System.Text.Json.JsonSerializerOptions.Default)!);

        builder
            .HasIndex(x => x.Name)
            .IsUnique();
    }
}