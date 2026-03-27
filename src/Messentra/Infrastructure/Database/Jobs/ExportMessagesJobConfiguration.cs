using Messentra.Features.Jobs.ExportMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class ExportMessagesJobConfiguration : IEntityTypeConfiguration<ExportMessagesJob>
{
    public void Configure(EntityTypeBuilder<ExportMessagesJob> builder)
    {
        builder.Ignore(x => x.Input);
        builder.Ignore(x => x.Output);
    }
}