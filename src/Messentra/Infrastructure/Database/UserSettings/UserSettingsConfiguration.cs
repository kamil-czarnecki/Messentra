using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messentra.Infrastructure.Database.UserSettings;

public class UserSettingsConfiguration : IEntityTypeConfiguration<Domain.UserSettings>
{
    public void Configure(EntityTypeBuilder<Domain.UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IsDarkMode).IsRequired();
    }
}
