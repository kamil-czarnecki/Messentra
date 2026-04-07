using Messentra.Features.Jobs.ExportSelectedMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class ExportSelectedMessagesJobConfiguration : IEntityTypeConfiguration<ExportSelectedMessagesJob>
{
    public void Configure(EntityTypeBuilder<ExportSelectedMessagesJob> builder)
    {
        builder.Ignore(x => x.Input);
        builder.Ignore(x => x.Output);
    }
}
