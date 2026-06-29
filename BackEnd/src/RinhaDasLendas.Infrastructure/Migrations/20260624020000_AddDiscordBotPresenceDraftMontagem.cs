using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations
{
    public partial class AddDiscordBotPresenceDraftMontagem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "discord_guild_id", table: "draft_montagens", type: "character varying(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(name: "discord_presence_message_id", table: "draft_montagens", type: "character varying(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "horario_encerramento_presenca", table: "draft_montagens", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ordem_escolha_modo", table: "draft_montagens", type: "character varying(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "presenca_continuada_manualmente", table: "draft_montagens", type: "boolean", nullable: false, defaultValue: false);

            migrationBuilder.CreateTable(
                name: "discord_server_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guild_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    presence_channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    news_channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    admin_channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    draft_channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    match_result_channel_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    bot_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("pk_discord_server_configurations", x => x.id));

            migrationBuilder.CreateTable(
                name: "draft_montagem_presencas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discord_user_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    origem_confirmacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cancelado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ordem_confirmacao = table.Column<int>(type: "integer", nullable: false),
                    ordem_manual = table.Column<int>(type: "integer", nullable: true),
                    ordem_final = table.Column<int>(type: "integer", nullable: true),
                    data_cadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_draft_montagem_presencas", x => x.id);
                    table.ForeignKey("fk_draft_montagem_presencas_draft_montagens_draft_montagem_id", x => x.draft_montagem_id, "draft_montagens", "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("fk_draft_montagem_presencas_jogadores_jogador_id", x => x.jogador_id, "jogadores", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("fk_draft_montagem_presencas_usuarios_usuario_id", x => x.usuario_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "ix_discord_server_configurations_guild_id", table: "discord_server_configurations", column: "guild_id", unique: true);
            migrationBuilder.CreateIndex(name: "ix_draft_montagens_status_horario_encerramento_presenca", table: "draft_montagens", columns: new[] { "status", "horario_encerramento_presenca" });
            migrationBuilder.CreateIndex(name: "ix_draft_montagem_presencas_draft_montagem_id", table: "draft_montagem_presencas", column: "draft_montagem_id");
            migrationBuilder.CreateIndex(name: "ix_draft_montagem_presencas_jogador_id", table: "draft_montagem_presencas", column: "jogador_id");
            migrationBuilder.CreateIndex(name: "ix_draft_montagem_presencas_usuario_id", table: "draft_montagem_presencas", column: "usuario_id");
            migrationBuilder.CreateIndex(name: "ix_draft_montagem_presencas_draft_montagem_id_usuario_id", table: "draft_montagem_presencas", columns: new[] { "draft_montagem_id", "usuario_id" }, unique: true, filter: "status = 'Confirmada'");
            migrationBuilder.CreateIndex(name: "ix_draft_montagem_presencas_draft_montagem_id_jogador_id", table: "draft_montagem_presencas", columns: new[] { "draft_montagem_id", "jogador_id" }, unique: true, filter: "status = 'Confirmada'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "discord_server_configurations");
            migrationBuilder.DropTable(name: "draft_montagem_presencas");
            migrationBuilder.DropIndex(name: "ix_draft_montagens_status_horario_encerramento_presenca", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "discord_guild_id", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "discord_presence_message_id", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "horario_encerramento_presenca", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "ordem_escolha_modo", table: "draft_montagens");
            migrationBuilder.DropColumn(name: "presenca_continuada_manualmente", table: "draft_montagens");
        }
    }
}
