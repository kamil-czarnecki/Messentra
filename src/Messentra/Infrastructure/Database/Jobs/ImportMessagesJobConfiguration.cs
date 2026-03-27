using Messentra.Features.Jobs.ImportMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Jobs;

public sealed class ImportMessagesJobConfiguration : IEntityTypeConfiguration<ImportMessagesJob>
{
    public void Configure(EntityTypeBuilder<ImportMessagesJob> builder)
    {
        builder.Ignore(x => x.Input);
        builder.Ignore(x => x.Output);
    }
}