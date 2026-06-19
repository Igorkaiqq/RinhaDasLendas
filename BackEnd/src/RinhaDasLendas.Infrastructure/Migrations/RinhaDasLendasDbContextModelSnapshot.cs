using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
public partial class RinhaDasLendasDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.8");

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.Jogador", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<DateTimeOffset>("DataAtualizacao").HasColumnName("data_atualizacao");
            b.Property<DateTimeOffset>("DataCadastro").HasColumnName("data_cadastro");
            b.Property<string>("DeepLolUrl").HasMaxLength(500).HasColumnName("deep_lol_url");
            b.Property<string>("Discord").HasMaxLength(120).HasColumnName("discord");
            b.Property<string>("Elo").IsRequired().HasMaxLength(30).HasColumnName("elo");
            b.Property<string>("Divisao").HasMaxLength(5).HasColumnName("divisao");
            b.Property<string>("NomeExibicao").IsRequired().HasMaxLength(100).HasColumnName("nome_exibicao");
            b.Property<string>("NomeReal").HasMaxLength(120).HasColumnName("nome_real");
            b.Property<string>("OpGgUrl").HasMaxLength(500).HasColumnName("op_gg_url");
            b.Property<string>("RiotId").HasMaxLength(120).HasColumnName("riot_id");
            b.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnName("status");
            b.HasKey("Id");
            b.ToTable("jogadores");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.PreferenciaRota", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<Guid>("JogadorId").HasColumnName("jogador_id");
            b.Property<bool>("NaoJogoNemLascando").HasDefaultValue(false).HasColumnName("nao_jogo_nem_lascando");
            b.Property<int>("Prioridade").HasColumnName("prioridade");
            b.Property<string>("Rota").IsRequired().HasMaxLength(20).HasColumnName("rota");
            b.HasKey("Id");
            b.HasIndex("JogadorId");
            b.HasIndex("JogadorId", "Rota").IsUnique();
            b.ToTable("preferencias_rotas", t => t.HasCheckConstraint("ck_preferencias_rotas_prioridade", "prioridade >= 1 AND prioridade <= 5"));
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.Time", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<Guid?>("CapitaoId").HasColumnName("capitao_id");
            b.Property<DateTimeOffset>("DataAtualizacao").HasColumnName("data_atualizacao");
            b.Property<DateTimeOffset>("DataCadastro").HasColumnName("data_cadastro");
            b.Property<string>("Nome").IsRequired().HasMaxLength(100).HasColumnName("nome");
            b.Property<string>("NomeNormalizado").IsRequired().HasMaxLength(100).HasColumnName("nome_normalizado");
            b.Property<string>("Observacoes").HasMaxLength(500).HasColumnName("observacoes");
            b.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnName("status");
            b.Property<string>("Tag").IsRequired().HasMaxLength(10).HasColumnName("tag");
            b.Property<string>("TagNormalizada").IsRequired().HasMaxLength(10).HasColumnName("tag_normalizada");
            b.HasKey("Id");
            b.HasIndex("CapitaoId");
            b.HasIndex("NomeNormalizado").IsUnique().HasFilter("status = 'Ativo'");
            b.HasIndex("Status");
            b.HasIndex("TagNormalizada").IsUnique().HasFilter("status = 'Ativo'");
            b.ToTable("times");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.TimeMembro", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<DateTimeOffset>("DataCadastro").HasColumnName("data_cadastro");
            b.Property<Guid>("JogadorId").HasColumnName("jogador_id");
            b.Property<bool>("Principal").HasDefaultValue(true).HasColumnName("principal");
            b.Property<Guid>("TimeId").HasColumnName("time_id");
            b.HasKey("Id");
            b.HasIndex("JogadorId");
            b.HasIndex("TimeId");
            b.HasIndex("TimeId", "JogadorId").IsUnique();
            b.ToTable("time_membros");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftSessao", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<Guid>("CapitaoTimeAId").HasColumnName("capitao_time_a_id");
            b.Property<Guid>("CapitaoTimeBId").HasColumnName("capitao_time_b_id");
            b.Property<string>("CriterioCapitaes").IsRequired().HasMaxLength(20).HasColumnName("criterio_capitaes");
            b.Property<string>("CriterioPrimeiroPick").IsRequired().HasMaxLength(20).HasColumnName("criterio_primeiro_pick");
            b.Property<DateTimeOffset>("DataAtualizacao").HasColumnName("data_atualizacao");
            b.Property<DateTimeOffset>("DataCadastro").HasColumnName("data_cadastro");
            b.Property<string>("MotivoCancelamento").HasMaxLength(500).HasColumnName("motivo_cancelamento");
            b.Property<string>("Nome").IsRequired().HasMaxLength(120).HasColumnName("nome");
            b.Property<string>("Observacoes").HasMaxLength(500).HasColumnName("observacoes");
            b.Property<string>("ProximoTime").HasMaxLength(20).HasColumnName("proximo_time");
            b.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnName("status");
            b.Property<int>("TamanhoTime").HasColumnName("tamanho_time");
            b.HasKey("Id");
            b.HasIndex("CapitaoTimeAId");
            b.HasIndex("CapitaoTimeBId");
            b.HasIndex("DataCadastro");
            b.HasIndex("Status");
            b.ToTable("drafts");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftParticipante", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<bool>("Capitao").HasDefaultValue(false).HasColumnName("capitao");
            b.Property<DateTimeOffset>("DataCadastro").HasColumnName("data_cadastro");
            b.Property<Guid>("DraftSessaoId").HasColumnName("draft_sessao_id");
            b.Property<Guid>("JogadorId").HasColumnName("jogador_id");
            b.Property<string>("Time").HasMaxLength(20).HasColumnName("time");
            b.HasKey("Id");
            b.HasIndex("JogadorId");
            b.HasIndex("DraftSessaoId", "JogadorId").IsUnique();
            b.ToTable("draft_participantes");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftEscolha", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<Guid>("CapitaoId").HasColumnName("capitao_id");
            b.Property<DateTimeOffset>("DataEscolha").HasColumnName("data_escolha");
            b.Property<Guid>("DraftSessaoId").HasColumnName("draft_sessao_id");
            b.Property<Guid>("JogadorId").HasColumnName("jogador_id");
            b.Property<int>("Sequencia").HasColumnName("sequencia");
            b.Property<string>("Time").IsRequired().HasMaxLength(20).HasColumnName("time");
            b.HasKey("Id");
            b.HasIndex("CapitaoId");
            b.HasIndex("JogadorId");
            b.HasIndex("DraftSessaoId", "JogadorId").IsUnique();
            b.HasIndex("DraftSessaoId", "Sequencia").IsUnique();
            b.ToTable("draft_escolhas");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.PreferenciaRota", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", null)
                .WithMany("Preferencias")
                .HasForeignKey("JogadorId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.Time", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", null)
                .WithMany()
                .HasForeignKey("CapitaoId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.TimeMembro", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", "Jogador")
                .WithMany()
                .HasForeignKey("JogadorId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("RinhaDasLendas.Domain.Entities.Time", null)
                .WithMany("Membros")
                .HasForeignKey("TimeId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Jogador");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftSessao", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", null)
                .WithMany()
                .HasForeignKey("CapitaoTimeAId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", null)
                .WithMany()
                .HasForeignKey("CapitaoTimeBId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftParticipante", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.DraftSessao", null)
                .WithMany("Participantes")
                .HasForeignKey("DraftSessaoId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", "Jogador")
                .WithMany()
                .HasForeignKey("JogadorId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Jogador");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftEscolha", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.DraftSessao", null)
                .WithMany("Escolhas")
                .HasForeignKey("DraftSessaoId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", "Capitao")
                .WithMany()
                .HasForeignKey("CapitaoId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", "Jogador")
                .WithMany()
                .HasForeignKey("JogadorId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Capitao");
            b.Navigation("Jogador");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.DraftSessao", b =>
        {
            b.Navigation("Escolhas");
            b.Navigation("Participantes");
        });

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.Time", b =>
        {
            b.Navigation("Membros");
        });
    }
}
