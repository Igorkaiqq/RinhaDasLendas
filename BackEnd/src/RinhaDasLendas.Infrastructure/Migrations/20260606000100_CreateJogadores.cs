using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
[Migration("20260606000100_CreateJogadores")]
public partial class CreateJogadores : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "jogadores",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nome_exibicao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                nome_real = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                discord = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                riot_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                op_gg_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                deep_lol_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                elo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_jogadores", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "preferencias_rotas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                rota = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                prioridade = table.Column<int>(type: "integer", nullable: false),
                nao_jogo_nem_lascando = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_preferencias_rotas", x => x.id);
                table.CheckConstraint("ck_preferencias_rotas_prioridade", "prioridade >= 1 AND prioridade <= 5");
                table.ForeignKey(
                    name: "fk_preferencias_rotas_jogadores_jogador_id",
                    column: x => x.jogador_id,
                    principalTable: "jogadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_preferencias_rotas_jogador_id",
            table: "preferencias_rotas",
            column: "jogador_id");

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

        migrationBuilder.CreateIndex(
            name: "ix_preferencias_rotas_jogador_id_rota",
            table: "preferencias_rotas",
            columns: new[] { "jogador_id", "rota" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "preferencias_rotas");
        migrationBuilder.DropTable(name: "jogadores");
    }
}
