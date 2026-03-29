using System.Text.Json;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DatabaseJsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class ImportedMessageConfiguration : IEntityTypeConfiguration<ImportedMessage>
{
    public void Configure(EntityTypeBuilder<ImportedMessage> builder)
    {
        builder.ToTable("ImportedMessages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.JobId).IsRequired();
        builder.Property(x => x.Position).IsRequired();
        builder.Property(x => x.IsSent).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.SentOn);

        builder.Property(x => x.Message)
            .HasConversion(
                v => JsonSerializer.Serialize(v, DatabaseJsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<ServiceBusMessageDto>(v, DatabaseJsonSerializerOptions.Default)!)
            .IsRequired();

        builder.HasIndex(x => new { x.JobId, x.Position }).IsUnique();
        builder.HasIndex(x => new { x.JobId, x.IsSent, x.Position });
    }
}

