using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence.Converters;

namespace TaskManager.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public const string StatusTransitionTrigger = "TR_Tasks_ValidateStatusTransition";

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        // Hay que avisarle a EF que la tabla tiene un trigger, desde EF Core 7 los INSERT y UPDATE
        // usan OUTPUT, y SQL Server no lo permite en tablas con triggers.
        builder.ToTable("Tasks", "dbo", table => table.HasTrigger(StatusTransitionTrigger));

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).UseIdentityColumn();

        builder.Property(task => task.Title)
            .HasMaxLength(TaskItem.TitleMaxLength)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(task => task.Status)
            .HasColumnName("StatusId")
            .HasConversion<byte>()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Tasks_Users");

        builder.Property(task => task.AdditionalInfo)
            .HasColumnName("AdditionalInfo")
            .HasColumnType("nvarchar(max)")
            .HasConversion(AdditionalInfoConverter, AdditionalInfoComparer);

        builder.Property(task => task.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(task => task.UpdatedAt).HasColumnType("datetime2(3)");

        // AllowedNextStatuses se calcula en memoria, no es una columna, la columna calculada
        // Priority tampoco se mapea porque solo la usa el procedimiento de búsqueda
        builder.Ignore(task => task.AllowedNextStatuses);
    }

    private static readonly ValueConverter<TaskAdditionalInfo?, string?> AdditionalInfoConverter = new(
        info => TaskAdditionalInfoJson.Serialize(info),
        json => TaskAdditionalInfoJson.Deserialize(json));

    // EF compara el JSON para saber si la información adicional cambió, sin esto actualizaría
    // la columna en cada guardado aunque nadie la haya tocado
    private static readonly ValueComparer<TaskAdditionalInfo?> AdditionalInfoComparer = new(
        (left, right) => TaskAdditionalInfoJson.Serialize(left) == TaskAdditionalInfoJson.Serialize(right),
        info => StringComparer.Ordinal.GetHashCode(TaskAdditionalInfoJson.Serialize(info) ?? string.Empty),
        info => info);
}
