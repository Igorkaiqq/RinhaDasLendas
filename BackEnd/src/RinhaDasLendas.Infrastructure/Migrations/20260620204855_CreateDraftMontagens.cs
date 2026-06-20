using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateDraftMontagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "draft_montagens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tamanho_equipe = table.Column<int>(type: "integer", nullable: false),
                    quantidade_times = table.Column<int>(type: "integer", nullable: false),
                    quantidade_reservas = table.Column<int>(type: "integer", nullable: false),
                    criterio_capitaes = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "draft_montagem_times",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    cor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    capitao_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_times", x => x.id);
                    table.ForeignKey(
                        name: "FK_draft_montagem_times_draft_montagens_draft_montagem_id",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_draft_montagem_times_jogadores_capitao_id",
                        column: x => x.capitao_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "draft_montagem_participantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capitao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    rota_contextual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_participantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_draft_montagem_participantes_draft_montagem_times_time_id",
                        column: x => x.time_id,
                        principalTable: "draft_montagem_times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_draft_montagem_participantes_draft_montagens_draft_montagem~",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_draft_montagem_participantes_jogadores_jogador_id",
                        column: x => x.jogador_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_participantes_draft_montagem_id_jogador_id",
                table: "draft_montagem_participantes",
                columns: new[] { "draft_montagem_id", "jogador_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_participantes_jogador_id",
                table: "draft_montagem_participantes",
                column: "jogador_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_participantes_time_id",
                table: "draft_montagem_participantes",
                column: "time_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_times_capitao_id",
                table: "draft_montagem_times",
                column: "capitao_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_times_draft_montagem_id_ordem",
                table: "draft_montagem_times",
                columns: new[] { "draft_montagem_id", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagens_data_cadastro",
                table: "draft_montagens",
                column: "data_cadastro");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagens_status",
                table: "draft_montagens",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_montagem_participantes");

            migrationBuilder.DropTable(
                name: "draft_montagem_times");

            migrationBuilder.DropTable(
                name: "draft_montagens");
        }
    }
}
