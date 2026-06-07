using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
[Migration("20260607000100_RemovePreferenciasRotasSwapIndexes")]
public partial class RemovePreferenciasRotasSwapIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_preferencias_rotas_jogador_id_nao_jogo_nem_lascando",
            table: "preferencias_rotas");

        migrationBuilder.DropIndex(
            name: "ix_preferencias_rotas_jogador_id_prioridade",
            table: "preferencias_rotas");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_preferencias_rotas_jogador_id_nao_jogo_nem_lascando",
            table: "preferencias_rotas",
            columns: new[] { "jogador_id", "nao_jogo_nem_lascando" },
            unique: true,
            filter: "nao_jogo_nem_lascando = true");

        migrationBuilder.CreateIndex(
            name: "ix_preferencias_rotas_jogador_id_prioridade",
            table: "preferencias_rotas",
            columns: new[] { "jogador_id", "prioridade" },
            unique: true);
    }
}
