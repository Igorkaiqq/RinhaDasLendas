using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class RinhaDasLendasDbContext(DbContextOptions<RinhaDasLendasDbContext> options) : DbContext(options)
{
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<PreferenciaRota> PreferenciasRotas => Set<PreferenciaRota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Jogador>(entity =>
        {
            entity.ToTable("jogadores");
            entity.HasKey(jogador => jogador.Id);

            entity.Property(jogador => jogador.Id).HasColumnName("id");
            entity.Property(jogador => jogador.NomeExibicao).HasColumnName("nome_exibicao").HasMaxLength(100).IsRequired();
            entity.Property(jogador => jogador.NomeReal).HasColumnName("nome_real").HasMaxLength(120);
            entity.Property(jogador => jogador.Discord).HasColumnName("discord").HasMaxLength(120);
            entity.Property(jogador => jogador.RiotId).HasColumnName("riot_id").HasMaxLength(120);
            entity.Property(jogador => jogador.OpGgUrl).HasColumnName("op_gg_url").HasMaxLength(500);
            entity.Property(jogador => jogador.DeepLolUrl).HasColumnName("deep_lol_url").HasMaxLength(500);
            entity.Property(jogador => jogador.Elo).HasColumnName("elo").HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(jogador => jogador.Divisao).HasColumnName("divisao").HasConversion<string>().HasMaxLength(5);
            entity.Property(jogador => jogador.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(jogador => jogador.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(jogador => jogador.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasMany(jogador => jogador.Preferencias)
                .WithOne()
                .HasForeignKey(preferencia => preferencia.JogadorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(jogador => jogador.Preferencias)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PreferenciaRota>(entity =>
        {
            entity.ToTable("preferencias_rotas", table =>
            {
                table.HasCheckConstraint("ck_preferencias_rotas_prioridade", "prioridade >= 1 AND prioridade <= 5");
            });

            entity.HasKey(preferencia => preferencia.Id);
            entity.Property(preferencia => preferencia.Id).HasColumnName("id");
            entity.Property(preferencia => preferencia.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(preferencia => preferencia.Rota).HasColumnName("rota").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(preferencia => preferencia.Prioridade).HasColumnName("prioridade").IsRequired();
            entity.Property(preferencia => preferencia.NaoJogoNemLascando).HasColumnName("nao_jogo_nem_lascando").HasDefaultValue(false).IsRequired();

            entity.HasIndex(preferencia => new { preferencia.JogadorId, preferencia.Rota }).IsUnique();
            entity.HasIndex(preferencia => preferencia.JogadorId);
        });
    }
}
