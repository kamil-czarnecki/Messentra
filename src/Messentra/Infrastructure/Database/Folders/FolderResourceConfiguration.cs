using Messentra.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Folders;

public sealed class FolderResourceConfiguration : IEntityTypeConfiguration<FolderResource>
{
    public void Configure(EntityTypeBuilder<FolderResource> builder)
    {
        builder.ToTable("FolderResources");
        builder.HasKey(x => new { x.FolderId, x.ResourceUrl });
        builder.Property(x => x.ResourceUrl).IsRequired();
    }
}