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

        modelBuilder.Entity("RinhaDasLendas.Domain.Entities.PreferenciaRota", b =>
        {
            b.HasOne("RinhaDasLendas.Domain.Entities.Jogador", null)
                .WithMany("Preferencias")
                .HasForeignKey("JogadorId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
