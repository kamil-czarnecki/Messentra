using Messentra.Domain;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ImportMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DatabaseJsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property("Type").HasMaxLength(100).IsRequired();
        builder
            .HasDiscriminator<string>("Type")
            .HasValue<ExportMessagesJob>("ExportMessagesJob")
            .HasValue<ImportMessagesJob>("ImportMessagesJob")
            .HasValue<ExportSelectedMessagesJob>("ExportSelectedMessagesJob");
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property("InputRaw").HasMaxLength(5000);
        builder.Property("OutputRaw").HasMaxLength(5000);
        builder.Property(x => x.Status).HasConversion<EnumToStringConverter<JobStatus>>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.StageProgress)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, DatabaseJsonSerializerOptions.Default),
                v => System.Text.Json.JsonSerializer.Deserialize<StageProgress>(v, DatabaseJsonSerializerOptions.Default)!)
            .IsRequired()
            .HasMaxLength(1000);
        builder.Property(x => x.CurrentStageIndex).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.RetryCount).IsRequired();
        builder.Property(x => x.MaxRetries).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.CompletedAt);

        builder.Ignore(x => x.Stages);
        builder.Ignore("ProgressReporter");
    }
}