using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
[Migration("20260619000100_CreateTimes")]
public partial class CreateTimes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "times",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                nome_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                tag = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                tag_normalizada = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                capitao_id = table.Column<Guid>(type: "uuid", nullable: true),
                data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_times", x => x.id);
                table.ForeignKey(
                    name: "fk_times_jogadores_capitao_id",
                    column: x => x.capitao_id,
                    principalTable: "jogadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "time_membros",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                time_id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                principal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_time_membros", x => x.id);
                table.ForeignKey(
                    name: "fk_time_membros_jogadores_jogador_id",
                    column: x => x.jogador_id,
                    principalTable: "jogadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_time_membros_times_time_id",
                    column: x => x.time_id,
                    principalTable: "times",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_times_capitao_id", table: "times", column: "capitao_id");
        migrationBuilder.CreateIndex(name: "ix_times_nome_normalizado", table: "times", column: "nome_normalizado", unique: true, filter: "status = 'Ativo'");
        migrationBuilder.CreateIndex(name: "ix_times_status", table: "times", column: "status");
        migrationBuilder.CreateIndex(name: "ix_times_tag_normalizada", table: "times", column: "tag_normalizada", unique: true, filter: "status = 'Ativo'");
        migrationBuilder.CreateIndex(name: "ix_time_membros_jogador_id", table: "time_membros", column: "jogador_id");
        migrationBuilder.CreateIndex(name: "ix_time_membros_time_id", table: "time_membros", column: "time_id");
        migrationBuilder.CreateIndex(name: "ix_time_membros_time_id_jogador_id", table: "time_membros", columns: new[] { "time_id", "jogador_id" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "time_membros");
        migrationBuilder.DropTable(name: "times");
    }
}
