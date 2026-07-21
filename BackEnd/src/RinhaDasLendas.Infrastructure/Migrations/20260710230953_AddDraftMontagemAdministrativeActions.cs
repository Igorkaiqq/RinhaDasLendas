using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftMontagemAdministrativeActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "draft_montagem_acoes_administrativas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    responsavel_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_montagem_acoes_administrativas", x => x.id);
                    table.ForeignKey(
                        name: "FK_draft_montagem_acoes_administrativas_draft_montagens_draft_~",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_draft_montagem_acoes_administrativas_usuarios_responsavel_u~",
                        column: x => x.responsavel_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_draft_montagem_id",
                table: "draft_montagem_acoes_administrativas",
                column: "draft_montagem_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_montagem_acoes_administrativas_responsavel_usuario_id",
                table: "draft_montagem_acoes_administrativas",
                column: "responsavel_usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_montagem_acoes_administrativas");
        }
    }
}
