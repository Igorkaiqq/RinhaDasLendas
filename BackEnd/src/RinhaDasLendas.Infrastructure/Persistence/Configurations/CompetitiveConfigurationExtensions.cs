using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal static class CompetitiveConfigurationExtensions
{
    internal static void ConfigureUuidPrimaryKey<TEntity>(this EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        entity.HasKey("Id");
        entity.Property<Guid>("Id").HasColumnName("id").ValueGeneratedNever();
    }

    internal static PropertyBuilder<DateTimeOffset> HasUtcInstant(
        this PropertyBuilder<DateTimeOffset> property,
        string columnName) =>
        property.HasColumnName(columnName).HasColumnType("timestamp with time zone");

    internal static PropertyBuilder<DateTimeOffset?> HasNullableUtcInstant(
        this PropertyBuilder<DateTimeOffset?> property,
        string columnName) =>
        property.HasColumnName(columnName).HasColumnType("timestamp with time zone");
}
