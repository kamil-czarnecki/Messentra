using Messentra.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.Folders;

public sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("Folders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ConnectionId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasOne<Connection>()
            .WithMany()
            .HasForeignKey(x => x.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Resources)
            .WithOne()
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}