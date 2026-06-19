using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class RinhaDasLendasDbContext(DbContextOptions<RinhaDasLendasDbContext> options) : DbContext(options)
{
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<PreferenciaRota> PreferenciasRotas => Set<PreferenciaRota>();
    public DbSet<Time> Times => Set<Time>();
    public DbSet<TimeMembro> TimeMembros => Set<TimeMembro>();
    public DbSet<DraftSessao> Drafts => Set<DraftSessao>();
    public DbSet<DraftParticipante> DraftParticipantes => Set<DraftParticipante>();
    public DbSet<DraftEscolha> DraftEscolhas => Set<DraftEscolha>();

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

        modelBuilder.Entity<Time>(entity =>
        {
            entity.ToTable("times");
            entity.HasKey(time => time.Id);

            entity.Property(time => time.Id).HasColumnName("id");
            entity.Property(time => time.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            entity.Property(time => time.NomeNormalizado).HasColumnName("nome_normalizado").HasMaxLength(100).IsRequired();
            entity.Property(time => time.Tag).HasColumnName("tag").HasMaxLength(10).IsRequired();
            entity.Property(time => time.TagNormalizada).HasColumnName("tag_normalizada").HasMaxLength(10).IsRequired();
            entity.Property(time => time.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
            entity.Property(time => time.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(time => time.CapitaoId).HasColumnName("capitao_id");
            entity.Property(time => time.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(time => time.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasMany(time => time.Membros)
                .WithOne()
                .HasForeignKey(membro => membro.TimeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Jogador>()
                .WithMany()
                .HasForeignKey(time => time.CapitaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(time => time.Status);
            entity.HasIndex(time => time.NomeNormalizado)
                .IsUnique()
                .HasFilter("status = 'Ativo'");
            entity.HasIndex(time => time.TagNormalizada)
                .IsUnique()
                .HasFilter("status = 'Ativo'");

            entity.Navigation(time => time.Membros)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TimeMembro>(entity =>
        {
            entity.ToTable("time_membros");
            entity.HasKey(membro => membro.Id);

            entity.Property(membro => membro.Id).HasColumnName("id");
            entity.Property(membro => membro.TimeId).HasColumnName("time_id").IsRequired();
            entity.Property(membro => membro.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(membro => membro.Principal).HasColumnName("principal").HasDefaultValue(true).IsRequired();
            entity.Property(membro => membro.DataCadastro).HasColumnName("data_cadastro").IsRequired();

            entity.HasOne(membro => membro.Jogador)
                .WithMany()
                .HasForeignKey(membro => membro.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(membro => new { membro.TimeId, membro.JogadorId }).IsUnique();
            entity.HasIndex(membro => membro.TimeId);
            entity.HasIndex(membro => membro.JogadorId);
        });

        modelBuilder.Entity<DraftSessao>(entity =>
        {
            entity.ToTable("drafts");
            entity.HasKey(draft => draft.Id);

            entity.Property(draft => draft.Id).HasColumnName("id");
            entity.Property(draft => draft.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            entity.Property(draft => draft.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
            entity.Property(draft => draft.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(draft => draft.TamanhoTime).HasColumnName("tamanho_time").IsRequired();
            entity.Property(draft => draft.CapitaoTimeAId).HasColumnName("capitao_time_a_id").IsRequired();
            entity.Property(draft => draft.CapitaoTimeBId).HasColumnName("capitao_time_b_id").IsRequired();
            entity.Property(draft => draft.CriterioCapitaes).HasColumnName("criterio_capitaes").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(draft => draft.ProximoTime).HasColumnName("proximo_time").HasConversion<string>().HasMaxLength(20);
            entity.Property(draft => draft.CriterioPrimeiroPick).HasColumnName("criterio_primeiro_pick").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(draft => draft.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
            entity.Property(draft => draft.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(draft => draft.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasOne<Jogador>()
                .WithMany()
                .HasForeignKey(draft => draft.CapitaoTimeAId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Jogador>()
                .WithMany()
                .HasForeignKey(draft => draft.CapitaoTimeBId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(draft => draft.Participantes)
                .WithOne()
                .HasForeignKey(participante => participante.DraftSessaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(draft => draft.Escolhas)
                .WithOne()
                .HasForeignKey(escolha => escolha.DraftSessaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(draft => draft.Status);
            entity.HasIndex(draft => draft.DataCadastro);

            entity.Navigation(draft => draft.Participantes).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(draft => draft.Escolhas).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DraftParticipante>(entity =>
        {
            entity.ToTable("draft_participantes");
            entity.HasKey(participante => participante.Id);

            entity.Property(participante => participante.Id).HasColumnName("id");
            entity.Property(participante => participante.DraftSessaoId).HasColumnName("draft_sessao_id").IsRequired();
            entity.Property(participante => participante.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(participante => participante.Time).HasColumnName("time").HasConversion<string>().HasMaxLength(20);
            entity.Property(participante => participante.Capitao).HasColumnName("capitao").HasDefaultValue(false).IsRequired();
            entity.Property(participante => participante.DataCadastro).HasColumnName("data_cadastro").IsRequired();

            entity.HasOne(participante => participante.Jogador)
                .WithMany()
                .HasForeignKey(participante => participante.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(participante => new { participante.DraftSessaoId, participante.JogadorId }).IsUnique();
            entity.HasIndex(participante => participante.JogadorId);
        });

        modelBuilder.Entity<DraftEscolha>(entity =>
        {
            entity.ToTable("draft_escolhas");
            entity.HasKey(escolha => escolha.Id);

            entity.Property(escolha => escolha.Id).HasColumnName("id");
            entity.Property(escolha => escolha.DraftSessaoId).HasColumnName("draft_sessao_id").IsRequired();
            entity.Property(escolha => escolha.Sequencia).HasColumnName("sequencia").IsRequired();
            entity.Property(escolha => escolha.Time).HasColumnName("time").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(escolha => escolha.CapitaoId).HasColumnName("capitao_id").IsRequired();
            entity.Property(escolha => escolha.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(escolha => escolha.DataEscolha).HasColumnName("data_escolha").IsRequired();

            entity.HasOne(escolha => escolha.Capitao)
                .WithMany()
                .HasForeignKey(escolha => escolha.CapitaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(escolha => escolha.Jogador)
                .WithMany()
                .HasForeignKey(escolha => escolha.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(escolha => new { escolha.DraftSessaoId, escolha.Sequencia }).IsUnique();
            entity.HasIndex(escolha => new { escolha.DraftSessaoId, escolha.JogadorId }).IsUnique();
        });
    }
}
