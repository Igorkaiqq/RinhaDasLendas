using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RinhaDasLendas.Domain.Entities;
using DomainTime = RinhaDasLendas.Domain.Entities.Time;

namespace RinhaDasLendas.Infrastructure.Persistence.Configurations;

internal sealed class EventoTimeConfiguration : IEntityTypeConfiguration<EventoTime>
{
    public void Configure(EntityTypeBuilder<EventoTime> entity)
    {
        entity.ToTable("evento_times", table =>
            table.HasCheckConstraint("ck_evento_times_ordem_valida", "ordem >= 1 AND ordem <= 4"));
        entity.ConfigureUuidPrimaryKey();
        entity.Property(team => team.EventoId).HasColumnName("evento_id").IsRequired();
        entity.Property(team => team.TimeId).HasColumnName("time_id").IsRequired();
        entity.Property(team => team.Ordem).HasColumnName("ordem").IsRequired();
        entity.Property(team => team.NomeSnapshot).HasColumnName("nome_snapshot").HasMaxLength(100).IsRequired();
        entity.Property(team => team.TagSnapshot).HasColumnName("tag_snapshot").HasMaxLength(10);
        entity.HasOne<EventoCompetitivo>().WithMany(@event => @event.Times).HasForeignKey(team => team.EventoId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_evento_times_evento_id");
        entity.HasOne<DomainTime>().WithMany().HasForeignKey(team => team.TimeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_evento_times_time_id");
        entity.HasIndex(team => team.TimeId).HasDatabaseName("ix_evento_times_time_id");
        entity.HasIndex(team => new { team.EventoId, team.Ordem }).IsUnique()
            .HasDatabaseName("ux_evento_times_evento_id_ordem");
        entity.HasIndex(team => new { team.EventoId, team.TimeId }).IsUnique()
            .HasDatabaseName("ux_evento_times_evento_id_time_id");
    }
}
