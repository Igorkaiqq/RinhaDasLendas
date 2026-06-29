using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class RinhaDasLendasDbContext(DbContextOptions<RinhaDasLendasDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<PreferenciaRota> PreferenciasRotas => Set<PreferenciaRota>();
    public DbSet<Time> Times => Set<Time>();
    public DbSet<TimeMembro> TimeMembros => Set<TimeMembro>();
    public DbSet<DraftSessao> Drafts => Set<DraftSessao>();
    public DbSet<DraftParticipante> DraftParticipantes => Set<DraftParticipante>();
    public DbSet<DraftEscolha> DraftEscolhas => Set<DraftEscolha>();
    public DbSet<DraftMontagem> DraftMontagens => Set<DraftMontagem>();
    public DbSet<DraftMontagemTime> DraftMontagemTimes => Set<DraftMontagemTime>();
    public DbSet<DraftMontagemParticipante> DraftMontagemParticipantes => Set<DraftMontagemParticipante>();
    public DbSet<DraftMontagemPresenca> DraftMontagemPresencas => Set<DraftMontagemPresenca>();
    public DbSet<DraftMontagemEscolha> DraftMontagemEscolhas => Set<DraftMontagemEscolha>();
    public DbSet<DraftMontagemSubstituicao> DraftMontagemSubstituicoes => Set<DraftMontagemSubstituicao>();
    public DbSet<DiscordServerConfiguration> DiscordServerConfigurations => Set<DiscordServerConfiguration>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalAccount> ExternalAccounts => Set<ExternalAccount>();
    public DbSet<ExternalAuthState> ExternalAuthStates => Set<ExternalAuthState>();
    public DbSet<VinculoDiscord> VinculosDiscord => Set<VinculoDiscord>();
    public DbSet<AuditoriaUsuario> AuditoriaUsuarios => Set<AuditoriaUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);

        modelBuilder.Entity<Jogador>(entity =>
        {
            entity.ToTable("jogadores");
            entity.HasKey(jogador => jogador.Id);

            entity.Property(jogador => jogador.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(jogador => jogador.UsuarioId).HasColumnName("usuario_id");
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

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(jogador => jogador.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(jogador => jogador.UsuarioId)
                .IsUnique()
                .HasFilter("usuario_id IS NOT NULL");
        });

        modelBuilder.Entity<PreferenciaRota>(entity =>
        {
            entity.ToTable("preferencias_rotas", table =>
            {
                table.HasCheckConstraint("ck_preferencias_rotas_prioridade", "prioridade >= 1 AND prioridade <= 5");
            });

            entity.HasKey(preferencia => preferencia.Id);
            entity.Property(preferencia => preferencia.Id).HasColumnName("id").ValueGeneratedNever();
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

            entity.Property(time => time.Id).HasColumnName("id").ValueGeneratedNever();
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

            entity.Property(membro => membro.Id).HasColumnName("id").ValueGeneratedNever();
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

            entity.Property(draft => draft.Id).HasColumnName("id").ValueGeneratedNever();
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

            entity.Property(participante => participante.Id).HasColumnName("id").ValueGeneratedNever();
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

            entity.Property(escolha => escolha.Id).HasColumnName("id").ValueGeneratedNever();
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

        modelBuilder.Entity<DraftMontagem>(entity =>
        {
            entity.ToTable("draft_montagens");
            entity.HasKey(montagem => montagem.Id);

            entity.Property(montagem => montagem.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(montagem => montagem.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            entity.Property(montagem => montagem.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
            entity.Property(montagem => montagem.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(montagem => montagem.Modo).HasColumnName("modo").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(montagem => montagem.TamanhoEquipe).HasColumnName("tamanho_equipe").IsRequired();
            entity.Property(montagem => montagem.QuantidadeTimes).HasColumnName("quantidade_times").IsRequired();
            entity.Property(montagem => montagem.QuantidadeReservas).HasColumnName("quantidade_reservas").IsRequired();
            entity.Property(montagem => montagem.CriterioCapitaes).HasColumnName("criterio_capitaes").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(montagem => montagem.TurnoAtualTimeId).HasColumnName("turno_atual_time_id");
            entity.Property(montagem => montagem.TurnoAtualCapitaoId).HasColumnName("turno_atual_capitao_id");
            entity.Property(montagem => montagem.TurnoSequencia).HasColumnName("turno_sequencia");
            entity.Property(montagem => montagem.TurnoIniciadoEm).HasColumnName("turno_iniciado_em");
            entity.Property(montagem => montagem.TurnoExpiraEm).HasColumnName("turno_expira_em");
            entity.Property(montagem => montagem.DuracaoTurnoSegundos).HasColumnName("duracao_turno_segundos").IsRequired();
            entity.Property(montagem => montagem.VersaoEstado).HasColumnName("versao_estado").IsConcurrencyToken().IsRequired();
            entity.Property(montagem => montagem.HorarioEncerramentoPresenca).HasColumnName("horario_encerramento_presenca");
            entity.Property(montagem => montagem.DiscordGuildId).HasColumnName("discord_guild_id").HasMaxLength(40);
            entity.Property(montagem => montagem.DiscordPresenceMessageId).HasColumnName("discord_presence_message_id").HasMaxLength(40);
            entity.Property(montagem => montagem.OrdemEscolhaModo).HasColumnName("ordem_escolha_modo").HasConversion<string>().HasMaxLength(20);
            entity.Property(montagem => montagem.PresencaContinuadaManualmente).HasColumnName("presenca_continuada_manualmente").HasDefaultValue(false).IsRequired();
            entity.Property(montagem => montagem.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
            entity.Property(montagem => montagem.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(montagem => montagem.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasMany(montagem => montagem.Times)
                .WithOne()
                .HasForeignKey(time => time.DraftMontagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(montagem => montagem.Participantes)
                .WithOne()
                .HasForeignKey(participante => participante.DraftMontagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(montagem => montagem.Presencas)
                .WithOne()
                .HasForeignKey(presenca => presenca.DraftMontagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(montagem => montagem.Escolhas)
                .WithOne()
                .HasForeignKey(escolha => escolha.DraftMontagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(montagem => montagem.Substituicoes)
                .WithOne()
                .HasForeignKey(substituicao => substituicao.DraftMontagemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(montagem => montagem.Status);
            entity.HasIndex(montagem => new { montagem.Status, montagem.HorarioEncerramentoPresenca });
            entity.HasIndex(montagem => new { montagem.Status, montagem.Modo, montagem.TurnoExpiraEm });
            entity.HasIndex(montagem => montagem.DataCadastro);
            entity.Navigation(montagem => montagem.Times).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(montagem => montagem.Participantes).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(montagem => montagem.Presencas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(montagem => montagem.Escolhas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(montagem => montagem.Substituicoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DraftMontagemPresenca>(entity =>
        {
            entity.ToTable("draft_montagem_presencas");
            entity.HasKey(presenca => presenca.Id);
            entity.Property(presenca => presenca.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(presenca => presenca.DraftMontagemId).HasColumnName("draft_montagem_id").IsRequired();
            entity.Property(presenca => presenca.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entity.Property(presenca => presenca.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(presenca => presenca.DiscordUserId).HasColumnName("discord_user_id").HasMaxLength(40);
            entity.Property(presenca => presenca.OrigemConfirmacao).HasColumnName("origem_confirmacao").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(presenca => presenca.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(presenca => presenca.ConfirmadoEm).HasColumnName("confirmado_em").IsRequired();
            entity.Property(presenca => presenca.CanceladoEm).HasColumnName("cancelado_em");
            entity.Property(presenca => presenca.OrdemConfirmacao).HasColumnName("ordem_confirmacao").IsRequired();
            entity.Property(presenca => presenca.OrdemManual).HasColumnName("ordem_manual");
            entity.Property(presenca => presenca.OrdemFinal).HasColumnName("ordem_final");
            entity.Property(presenca => presenca.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(presenca => presenca.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasOne(presenca => presenca.Jogador)
                .WithMany()
                .HasForeignKey(presenca => presenca.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(presenca => presenca.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(presenca => presenca.DraftMontagemId);
            entity.HasIndex(presenca => presenca.UsuarioId);
            entity.HasIndex(presenca => presenca.JogadorId);
            entity.HasIndex(presenca => new { presenca.DraftMontagemId, presenca.UsuarioId })
                .IsUnique()
                .HasFilter("status = 'Confirmada'");
            entity.HasIndex(presenca => new { presenca.DraftMontagemId, presenca.JogadorId })
                .IsUnique()
                .HasFilter("status = 'Confirmada'");
        });

        modelBuilder.Entity<DraftMontagemTime>(entity =>
        {
            entity.ToTable("draft_montagem_times");
            entity.HasKey(time => time.Id);
            entity.Property(time => time.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(time => time.DraftMontagemId).HasColumnName("draft_montagem_id").IsRequired();
            entity.Property(time => time.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            entity.Property(time => time.Ordem).HasColumnName("ordem").IsRequired();
            entity.Property(time => time.Cor).HasColumnName("cor").HasMaxLength(30).IsRequired();
            entity.Property(time => time.CapitaoId).HasColumnName("capitao_id");

            entity.HasOne<Jogador>()
                .WithMany()
                .HasForeignKey(time => time.CapitaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(time => new { time.DraftMontagemId, time.Ordem }).IsUnique();
        });

        modelBuilder.Entity<DraftMontagemParticipante>(entity =>
        {
            entity.ToTable("draft_montagem_participantes");
            entity.HasKey(participante => participante.Id);
            entity.Property(participante => participante.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(participante => participante.DraftMontagemId).HasColumnName("draft_montagem_id").IsRequired();
            entity.Property(participante => participante.JogadorId).HasColumnName("jogador_id").IsRequired();
            entity.Property(participante => participante.TimeId).HasColumnName("time_id");
            entity.Property(participante => participante.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(participante => participante.Capitao).HasColumnName("capitao").HasDefaultValue(false).IsRequired();
            entity.Property(participante => participante.RotaContextual).HasColumnName("rota_contextual").HasConversion<string>().HasMaxLength(20);
            entity.Property(participante => participante.Ordem).HasColumnName("ordem").IsRequired();
            entity.Property(participante => participante.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(participante => participante.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

            entity.HasOne(participante => participante.Jogador)
                .WithMany()
                .HasForeignKey(participante => participante.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<DraftMontagemTime>()
                .WithMany()
                .HasForeignKey(participante => participante.TimeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(participante => new { participante.DraftMontagemId, participante.JogadorId }).IsUnique();
            entity.HasIndex(participante => participante.JogadorId);
            entity.HasIndex(participante => participante.TimeId);
        });

        modelBuilder.Entity<DraftMontagemEscolha>(entity =>
        {
            entity.ToTable("draft_montagem_escolhas");
            entity.HasKey(escolha => escolha.Id);
            entity.Property(escolha => escolha.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(escolha => escolha.DraftMontagemId).HasColumnName("draft_montagem_id").IsRequired();
            entity.Property(escolha => escolha.Sequencia).HasColumnName("sequencia").IsRequired();
            entity.Property(escolha => escolha.TimeId).HasColumnName("time_id").IsRequired();
            entity.Property(escolha => escolha.CapitaoId).HasColumnName("capitao_id").IsRequired();
            entity.Property(escolha => escolha.JogadorId).HasColumnName("jogador_id");
            entity.Property(escolha => escolha.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(escolha => escolha.RegistradoEm).HasColumnName("registrado_em").IsRequired();

            entity.HasOne<DraftMontagemTime>()
                .WithMany()
                .HasForeignKey(escolha => escolha.TimeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(escolha => escolha.Capitao)
                .WithMany()
                .HasForeignKey(escolha => escolha.CapitaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(escolha => escolha.Jogador)
                .WithMany()
                .HasForeignKey(escolha => escolha.JogadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(escolha => new { escolha.DraftMontagemId, escolha.Sequencia }).IsUnique();
            entity.HasIndex(escolha => escolha.TimeId);
            entity.HasIndex(escolha => escolha.CapitaoId);
            entity.HasIndex(escolha => escolha.JogadorId);
        });

        modelBuilder.Entity<DraftMontagemSubstituicao>(entity =>
        {
            entity.ToTable("draft_montagem_substituicoes");
            entity.HasKey(substituicao => substituicao.Id);
            entity.Property(substituicao => substituicao.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(substituicao => substituicao.DraftMontagemId).HasColumnName("draft_montagem_id").IsRequired();
            entity.Property(substituicao => substituicao.TimeId).HasColumnName("time_id").IsRequired();
            entity.Property(substituicao => substituicao.JogadorSaiuId).HasColumnName("jogador_saiu_id").IsRequired();
            entity.Property(substituicao => substituicao.ReservaEntrouId).HasColumnName("reserva_entrou_id").IsRequired();
            entity.Property(substituicao => substituicao.Motivo).HasColumnName("motivo").HasMaxLength(500);
            entity.Property(substituicao => substituicao.ResponsavelUsuarioId).HasColumnName("responsavel_usuario_id").IsRequired();
            entity.Property(substituicao => substituicao.RegistradoEm).HasColumnName("registrado_em").IsRequired();

            entity.HasOne<DraftMontagemTime>()
                .WithMany()
                .HasForeignKey(substituicao => substituicao.TimeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(substituicao => substituicao.JogadorSaiu)
                .WithMany()
                .HasForeignKey(substituicao => substituicao.JogadorSaiuId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(substituicao => substituicao.ReservaEntrou)
                .WithMany()
                .HasForeignKey(substituicao => substituicao.ReservaEntrouId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(substituicao => substituicao.ResponsavelUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(substituicao => substituicao.DraftMontagemId);
            entity.HasIndex(substituicao => substituicao.TimeId);
            entity.HasIndex(substituicao => substituicao.ReservaEntrouId);
        });
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("usuarios");
            entity.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(user => user.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            entity.Property(user => user.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
            entity.Property(user => user.UltimoLoginEm).HasColumnName("ultimo_login_em");
            entity.Property(user => user.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.Property(user => user.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();
            entity.Property(user => user.UserName).HasColumnName("user_name").HasMaxLength(256);
            entity.Property(user => user.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(256);
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
            entity.Property(user => user.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash");
            entity.Property(user => user.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(user => user.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(user => user.PhoneNumber).HasColumnName("phone_number");
            entity.Property(user => user.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(user => user.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(user => user.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(user => user.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(user => user.AccessFailedCount).HasColumnName("access_failed_count");
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasIndex(user => user.Ativo);
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(role => role.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(role => role.NormalizedName).HasColumnName("normalized_name").HasMaxLength(120).IsRequired();
            entity.Property(role => role.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(role => role.NivelHierarquico).HasColumnName("nivel_hierarquico").IsRequired();
            entity.HasIndex(role => role.NormalizedName).IsUnique();
            entity.HasData(AuthRoles.Levels.Select(role => new ApplicationRole
            {
                Id = RoleId(role.Key),
                Name = role.Key,
                NormalizedName = role.Key.ToUpperInvariant(),
                NivelHierarquico = role.Value,
                ConcurrencyStamp = RoleId(role.Key).ToString(),
            }));
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("usuario_roles");
            entity.Property(userRole => userRole.UserId).HasColumnName("usuario_id");
            entity.Property(userRole => userRole.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("usuario_claims");
            entity.Property(claim => claim.Id).HasColumnName("id");
            entity.Property(claim => claim.UserId).HasColumnName("usuario_id");
            entity.Property(claim => claim.ClaimType).HasColumnName("claim_type");
            entity.Property(claim => claim.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("usuario_logins");
            entity.Property(login => login.LoginProvider).HasColumnName("login_provider");
            entity.Property(login => login.ProviderKey).HasColumnName("provider_key");
            entity.Property(login => login.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(login => login.UserId).HasColumnName("usuario_id");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("usuario_tokens");
            entity.Property(token => token.UserId).HasColumnName("usuario_id");
            entity.Property(token => token.LoginProvider).HasColumnName("login_provider");
            entity.Property(token => token.Name).HasColumnName("name");
            entity.Property(token => token.Value).HasColumnName("value");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
            entity.Property(claim => claim.Id).HasColumnName("id");
            entity.Property(claim => claim.RoleId).HasColumnName("role_id");
            entity.Property(claim => claim.ClaimType).HasColumnName("claim_type");
            entity.Property(claim => claim.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(token => token.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(token => token.FamiliaId).HasColumnName("familia_id").IsRequired();
            entity.Property(token => token.CriadoEm).HasColumnName("criado_em").IsRequired();
            entity.Property(token => token.ExpiraEm).HasColumnName("expira_em").IsRequired();
            entity.Property(token => token.RevogadoEm).HasColumnName("revogado_em");
            entity.Property(token => token.SubstituidoPorTokenId).HasColumnName("substituido_por_token_id");
            entity.Property(token => token.IpCriacao).HasColumnName("ip_criacao").HasMaxLength(64);
            entity.Property(token => token.UserAgentCriacao).HasColumnName("user_agent_criacao").HasMaxLength(500);
            entity.Property(token => token.IpRevogacao).HasColumnName("ip_revogacao").HasMaxLength(64);
            entity.Property(token => token.MotivoRevogacao).HasColumnName("motivo_revogacao").HasMaxLength(200);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UsuarioId);
            entity.HasIndex(token => token.FamiliaId);
        });

        modelBuilder.Entity<ExternalAccount>(entity =>
        {
            entity.ToTable("external_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(account => account.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entity.Property(account => account.Provider).HasColumnName("provider").HasMaxLength(40).IsRequired();
            entity.Property(account => account.ProviderUserId).HasColumnName("provider_user_id").HasMaxLength(120).IsRequired();
            entity.Property(account => account.Username).HasColumnName("username").HasMaxLength(120);
            entity.Property(account => account.DisplayName).HasColumnName("display_name").HasMaxLength(120);
            entity.Property(account => account.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(account => account.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
            entity.Property(account => account.LinkedAt).HasColumnName("linked_at").IsRequired();
            entity.Property(account => account.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(account => account.UnlinkedAt).HasColumnName("unlinked_at");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(account => account.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(account => account.UsuarioId);
            entity.HasIndex(account => new { account.Provider, account.ProviderUserId })
                .IsUnique()
                .HasFilter("unlinked_at IS NULL");
            entity.HasIndex(account => new { account.UsuarioId, account.Provider })
                .IsUnique()
                .HasFilter("unlinked_at IS NULL");
        });

        modelBuilder.Entity<ExternalAuthState>(entity =>
        {
            entity.ToTable("external_auth_states");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(state => state.StateHash).HasColumnName("state_hash").HasMaxLength(128).IsRequired();
            entity.Property(state => state.Flow).HasColumnName("flow").HasMaxLength(20).IsRequired();
            entity.Property(state => state.UsuarioId).HasColumnName("usuario_id");
            entity.Property(state => state.ReturnUrl).HasColumnName("return_url").HasMaxLength(500);
            entity.Property(state => state.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(state => state.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(state => state.ConsumedAt).HasColumnName("consumed_at");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(state => state.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(state => state.StateHash).IsUnique();
            entity.HasIndex(state => state.ExpiresAt);
        });

        modelBuilder.Entity<VinculoDiscord>(entity =>
        {
            entity.ToTable("vinculos_discord");
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(link => link.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entity.Property(link => link.DiscordUserId).HasColumnName("discord_user_id").HasMaxLength(64).IsRequired();
            entity.Property(link => link.DiscordUsername).HasColumnName("discord_username").HasMaxLength(120);
            entity.Property(link => link.DiscordGlobalName).HasColumnName("discord_global_name").HasMaxLength(120);
            entity.Property(link => link.DiscordAvatarHash).HasColumnName("discord_avatar_hash").HasMaxLength(120);
            entity.Property(link => link.VinculadoEm).HasColumnName("vinculado_em").IsRequired();
            entity.Property(link => link.DesvinculadoEm).HasColumnName("desvinculado_em");
            entity.Property(link => link.Escopos).HasColumnName("escopos").HasMaxLength(500);
            entity.HasIndex(link => link.UsuarioId).IsUnique().HasFilter("desvinculado_em IS NULL");
            entity.HasIndex(link => link.DiscordUserId).IsUnique().HasFilter("desvinculado_em IS NULL");
        });

        modelBuilder.Entity<DiscordServerConfiguration>(entity =>
        {
            entity.ToTable("discord_server_configurations");
            entity.HasKey(configuration => configuration.Id);
            entity.Property(configuration => configuration.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(configuration => configuration.GuildId).HasColumnName("guild_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.PresenceChannelId).HasColumnName("presence_channel_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.NewsChannelId).HasColumnName("news_channel_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.AdminChannelId).HasColumnName("admin_channel_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.DraftChannelId).HasColumnName("draft_channel_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.MatchResultChannelId).HasColumnName("match_result_channel_id").HasMaxLength(40).IsRequired();
            entity.Property(configuration => configuration.BotEnabled).HasColumnName("bot_enabled").IsRequired();
            entity.Property(configuration => configuration.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(configuration => configuration.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(configuration => configuration.GuildId).IsUnique();
        });

        modelBuilder.Entity<AuditoriaUsuario>(entity =>
        {
            entity.ToTable("auditoria_usuarios");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(audit => audit.UsuarioAlvoId).HasColumnName("usuario_alvo_id");
            entity.Property(audit => audit.UsuarioExecutorId).HasColumnName("usuario_executor_id");
            entity.Property(audit => audit.Acao).HasColumnName("acao").HasMaxLength(80).IsRequired();
            entity.Property(audit => audit.Detalhes).HasColumnName("detalhes");
            entity.Property(audit => audit.Ip).HasColumnName("ip").HasMaxLength(64);
            entity.Property(audit => audit.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            entity.Property(audit => audit.DataCadastro).HasColumnName("data_cadastro").IsRequired();
            entity.HasIndex(audit => audit.UsuarioAlvoId);
            entity.HasIndex(audit => audit.UsuarioExecutorId);
            entity.HasIndex(audit => audit.DataCadastro);
        });
    }

    private static Guid RoleId(string role)
    {
        return role switch
        {
            AuthRoles.SuperAdmin => Guid.Parse("10000000-0000-0000-0000-000000000001"),
            AuthRoles.Admin => Guid.Parse("10000000-0000-0000-0000-000000000002"),
            AuthRoles.Moderador => Guid.Parse("10000000-0000-0000-0000-000000000003"),
            AuthRoles.Capitao => Guid.Parse("10000000-0000-0000-0000-000000000004"),
            _ => Guid.Parse("10000000-0000-0000-0000-000000000005"),
        };
    }
}
