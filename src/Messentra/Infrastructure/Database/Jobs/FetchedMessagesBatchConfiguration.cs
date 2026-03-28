using System.Text.Json;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class FetchedMessagesBatchConfiguration : IEntityTypeConfiguration<FetchedMessagesBatch>
{
    public void Configure(EntityTypeBuilder<FetchedMessagesBatch> builder)
    {
        builder.ToTable("FetchedMessagesBatches");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.JobId).IsRequired();
        builder.Property(x => x.LastSequence).IsRequired();
        builder.Property(x => x.MessagesCount).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();

        builder.Property(x => x.Messages)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<ServiceBusMessageDto>>(v, JsonSerializerOptions.Default)!)
            .IsRequired();

        builder.HasIndex(x => new { x.JobId, x.LastSequence });
    }
}

