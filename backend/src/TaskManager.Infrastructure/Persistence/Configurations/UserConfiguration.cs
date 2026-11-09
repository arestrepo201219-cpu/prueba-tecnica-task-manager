using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Users;

namespace TaskManager.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).UseIdentityColumn();

        builder.Property(user => user.Name)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnType("datetime2(3)");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");
    }
}
