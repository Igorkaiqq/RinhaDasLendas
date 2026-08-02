using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RinhaDasLendas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitiveSeasonFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operacoes_idempotentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ator_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    rota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    chave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    recurso_tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resposta_minima = table.Column<string>(type: "text", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operacoes_idempotentes", x => x.id);
                    table.CheckConstraint("ck_operacoes_idempotentes_recurso_tipo_valido", "recurso_tipo IN ('CalendarioCompetitivo', 'Season', 'Competicao', 'Rodada', 'VersaoRegras', 'EventoCompetitivo', 'Serie', 'Partida')");
                    table.ForeignKey(
                        name: "fk_operacoes_idempotentes_ator_usuario_id",
                        column: x => x.ator_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_auditoria_competitiva",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recurso_tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ator_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    justificativa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_posterior = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_auditoria_competitiva", x => x.id);
                    table.CheckConstraint("ck_registros_auditoria_competitiva_acao_valida", "acao IN ('CalendarioCompetitivoAtualizado', 'TemporadaCriada', 'TemporadaAtualizada', 'TemporadaAtivada', 'TemporadaEncerrada', 'CompeticaoCriada', 'CompeticaoAtualizada', 'RodadaCriada', 'RodadasReordenadas', 'RegrasGeraisSeasonPublicadas', 'RegrasCompeticaoPublicadas', 'EventoCriado', 'EventoAtualizado', 'SerieAssociadaAoEvento', 'SerieCriada', 'SerieIniciada', 'PartidaAdicionada', 'PicksPartidaRegistrados', 'PartidaConfirmada', 'PartidaMarcadaComoRemake', 'PartidaCorrigida', 'PartidaAnulada', 'ResultadoSerieConfirmado', 'SerieCancelada', 'SerieAnulada', 'FatoCompetitivoCorrigido')");
                    table.CheckConstraint("ck_registros_auditoria_competitiva_recurso_tipo_valido", "recurso_tipo IN ('CalendarioCompetitivo', 'Season', 'Competicao', 'Rodada', 'VersaoRegras', 'EventoCompetitivo', 'Serie', 'Partida')");
                    table.ForeignKey(
                        name: "fk_registros_auditoria_competitiva_ator_usuario_id",
                        column: x => x.ator_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    ordem_no_ano = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim_exclusiva = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ativada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    encerrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.CheckConstraint("ck_seasons_ano_valido", "ano >= 2009 AND ano <= 9999");
                    table.CheckConstraint("ck_seasons_estado_valido", "estado IN ('Planejada', 'Ativa', 'Encerrada')");
                    table.CheckConstraint("ck_seasons_intervalo_valido", "data_fim_exclusiva > data_inicio");
                    table.CheckConstraint("ck_seasons_ordem_no_ano_positiva", "ordem_no_ano > 0");
                    table.ForeignKey(
                        name: "fk_seasons_atualizada_por_usuario_id",
                        column: x => x.atualizada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_seasons_criada_por_usuario_id",
                        column: x => x.criada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calendarios_competitivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_ativa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendarios_competitivos", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendarios_competitivos_atualizado_por_usuario_id",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calendarios_competitivos_season_ativa_id",
                        column: x => x.season_ativa_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competicoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    circuito_diario = table.Column<bool>(type: "boolean", nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competicoes", x => x.id);
                    table.UniqueConstraint("ak_competicoes_id_season_id", x => new { x.id, x.season_id });
                    table.CheckConstraint("ck_competicoes_id_nao_reservado", "id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "fk_competicoes_atualizada_por_usuario_id",
                        column: x => x.atualizada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competicoes_criada_por_usuario_id",
                        column: x => x.criada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competicoes_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_competitivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    modo_draft = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_competitivos", x => x.id);
                    table.UniqueConstraint("ak_eventos_competitivos_id_season_id", x => new { x.id, x.season_id });
                    table.CheckConstraint("ck_eventos_competitivos_modo_draft_padrao", "modo_draft = 'Padrao'");
                    table.ForeignKey(
                        name: "fk_eventos_competitivos_atualizado_por_usuario_id",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_competitivos_criado_por_usuario_id",
                        column: x => x.criado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_competitivos_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rodadas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rodadas", x => x.id);
                    table.UniqueConstraint("ak_rodadas_id_competicao_id", x => new { x.id, x.competicao_id });
                    table.CheckConstraint("ck_rodadas_ordem_positiva", "ordem > 0");
                    table.ForeignKey(
                        name: "fk_rodadas_competicao_id",
                        column: x => x.competicao_id,
                        principalTable: "competicoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "versoes_regras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_escopo_id = table.Column<Guid>(type: "uuid", nullable: false, computedColumnSql: "COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)", stored: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    modo_draft = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publicada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    publicada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_versoes_regras", x => x.id);
                    table.UniqueConstraint("ak_versoes_regras_id_competicao_escopo_id", x => new { x.id, x.competicao_escopo_id });
                    table.UniqueConstraint("ak_versoes_regras_id_season_id", x => new { x.id, x.season_id });
                    table.CheckConstraint("ck_versoes_regras_formato_valido", "formato IN ('Md3', 'Md5')");
                    table.CheckConstraint("ck_versoes_regras_modo_draft_valido", "modo_draft IN ('Padrao', 'Fearless')");
                    table.CheckConstraint("ck_versoes_regras_numero_positivo", "numero > 0");
                    table.ForeignKey(
                        name: "fk_versoes_regras_competicao_id",
                        columns: x => new { x.competicao_id, x.season_id },
                        principalTable: "competicoes",
                        principalColumns: new[] { "id", "season_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_versoes_regras_publicada_por_usuario_id",
                        column: x => x.publicada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_versoes_regras_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_times",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    nome_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tag_snapshot = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_times", x => x.id);
                    table.CheckConstraint("ck_evento_times_ordem_valida", "ordem >= 1 AND ordem <= 4");
                    table.ForeignKey(
                        name: "fk_evento_times_evento_id",
                        column: x => x.evento_id,
                        principalTable: "eventos_competitivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_times_time_id",
                        column: x => x.time_id,
                        principalTable: "times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lados_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    origem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tag_snapshot = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    capitao_jogador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    capitao_nome_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lados_series", x => x.id);
                    table.UniqueConstraint("ak_lados_series_id_serie_id", x => new { x.id, x.serie_id });
                    table.CheckConstraint("ck_lados_series_ordem_valida", "ordem >= 1 AND ordem <= 2");
                    table.CheckConstraint("ck_lados_series_tipo_valido", "tipo IN ('Temporario', 'TimeOficial')");
                    table.ForeignKey(
                        name: "fk_lados_series_capitao_jogador_id",
                        column: x => x.capitao_jogador_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participantes_esperados_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lado_serie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participantes_esperados_series", x => x.id);
                    table.CheckConstraint("ck_participantes_esperados_series_ordem_positiva", "ordem > 0");
                    table.ForeignKey(
                        name: "fk_participantes_esperados_series_jogador_id",
                        column: x => x.jogador_id,
                        principalTable: "jogadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_participantes_esperados_series_lado_serie_id",
                        column: x => x.lado_serie_id,
                        principalTable: "lados_series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_escopo_id = table.Column<Guid>(type: "uuid", nullable: false, computedColumnSql: "COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)", stored: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competicao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rodada_id = table.Column<Guid>(type: "uuid", nullable: true),
                    versao_regras_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_montagem_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    modo_draft = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fearless_habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    agendada_para = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_local = table.Column<DateOnly>(type: "date", nullable: true),
                    lado_vencedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revisao_necessaria = table.Column<bool>(type: "boolean", nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series", x => x.id);
                    table.CheckConstraint("ck_series_estado_valido", "estado IN ('Agendada', 'EmAndamento', 'Concluida', 'Cancelada', 'Anulada')");
                    table.CheckConstraint("ck_series_formato_valido", "formato IN ('Md3', 'Md5')");
                    table.CheckConstraint("ck_series_modo_draft_valido", "modo_draft IN ('Padrao', 'Fearless')");
                    table.CheckConstraint("ck_series_tipo_valido", "tipo IN ('DiariaTemporaria', 'ConfrontoOficial', 'Amistoso')");
                    table.ForeignKey(
                        name: "fk_series_atualizada_por_usuario_id",
                        column: x => x.atualizada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_competicao_id",
                        columns: x => new { x.competicao_id, x.season_id },
                        principalTable: "competicoes",
                        principalColumns: new[] { "id", "season_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_criada_por_usuario_id",
                        column: x => x.criada_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_draft_montagem_id",
                        column: x => x.draft_montagem_id,
                        principalTable: "draft_montagens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_evento_id",
                        columns: x => new { x.evento_id, x.season_id },
                        principalTable: "eventos_competitivos",
                        principalColumns: new[] { "id", "season_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_lado_vencedor_id",
                        columns: x => new { x.lado_vencedor_id, x.id },
                        principalTable: "lados_series",
                        principalColumns: new[] { "id", "serie_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_rodada_id",
                        columns: x => new { x.rodada_id, x.competicao_id },
                        principalTable: "rodadas",
                        principalColumns: new[] { "id", "competicao_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_versao_regras_competicao_id",
                        columns: x => new { x.versao_regras_id, x.competicao_escopo_id },
                        principalTable: "versoes_regras",
                        principalColumns: new[] { "id", "competicao_escopo_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_versao_regras_id",
                        columns: x => new { x.versao_regras_id, x.season_id },
                        principalTable: "versoes_regras",
                        principalColumns: new[] { "id", "season_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lado_vencedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_termino = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    decisao_picks_remake = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    conflito_fearless = table.Column<bool>(type: "boolean", nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partidas", x => x.id);
                    table.UniqueConstraint("ak_partidas_id_serie_id", x => new { x.id, x.serie_id });
                    table.CheckConstraint("ck_partidas_decisao_picks_remake_valida", "decisao_picks_remake IS NULL OR decisao_picks_remake IN ('PreservarPicks', 'DesconsiderarPicks')");
                    table.CheckConstraint("ck_partidas_estado_valido", "estado IN ('Rascunho', 'Confirmada', 'Remake', 'Anulada')");
                    table.CheckConstraint("ck_partidas_motivo_termino_valido", "motivo_termino IS NULL OR motivo_termino IN ('Normal', 'Surrender')");
                    table.CheckConstraint("ck_partidas_ordem_positiva", "ordem > 0");
                    table.ForeignKey(
                        name: "fk_partidas_lado_vencedor_id",
                        columns: x => new { x.lado_vencedor_id, x.serie_id },
                        principalTable: "lados_series",
                        principalColumns: new[] { "id", "serie_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_partidas_serie_id",
                        column: x => x.serie_id,
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "picks_partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lado_serie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    champion_id = table.Column<int>(type: "integer", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    versao_fato = table.Column<int>(type: "integer", nullable: false),
                    valido = table.Column<bool>(type: "boolean", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    serie_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_picks_partidas", x => x.id);
                    table.CheckConstraint("ck_picks_partidas_champion_id_positivo", "champion_id > 0");
                    table.CheckConstraint("ck_picks_partidas_ordem_valida", "ordem >= 1 AND ordem <= 5");
                    table.CheckConstraint("ck_picks_partidas_versao_fato_positiva", "versao_fato > 0");
                    table.ForeignKey(
                        name: "fk_picks_partidas_lado_serie_id",
                        columns: x => new { x.lado_serie_id, x.serie_id },
                        principalTable: "lados_series",
                        principalColumns: new[] { "id", "serie_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_picks_partidas_partida_id",
                        columns: x => new { x.partida_id, x.serie_id },
                        principalTable: "partidas",
                        principalColumns: new[] { "id", "serie_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "nivel_hierarquico",
                value: 700);

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "name", "nivel_hierarquico", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000006"), "10000000-0000-0000-0000-000000000006", "Presidente", 600, "PRESIDENTE" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "10000000-0000-0000-0000-000000000007", "VicePresidente", 500, "VICEPRESIDENTE" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendarios_competitivos_atualizado_por_usuario_id",
                table: "calendarios_competitivos",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendarios_competitivos_season_ativa_id",
                table: "calendarios_competitivos",
                column: "season_ativa_id");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_calendarios_competitivos_singleton ON calendarios_competitivos ((true))");

            migrationBuilder.CreateIndex(
                name: "ix_competicoes_atualizada_por_usuario_id",
                table: "competicoes",
                column: "atualizada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_competicoes_criada_por_usuario_id",
                table: "competicoes",
                column: "criada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_competicoes_circuito_diario",
                table: "competicoes",
                column: "season_id",
                unique: true,
                filter: "circuito_diario");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_competicoes_season_id_codigo ON competicoes (season_id, lower(codigo))");

            migrationBuilder.CreateIndex(
                name: "ix_evento_times_time_id",
                table: "evento_times",
                column: "time_id");

            migrationBuilder.CreateIndex(
                name: "ux_evento_times_evento_id_ordem",
                table: "evento_times",
                columns: new[] { "evento_id", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_evento_times_evento_id_time_id",
                table: "evento_times",
                columns: new[] { "evento_id", "time_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventos_competitivos_atualizado_por_usuario_id",
                table: "eventos_competitivos",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_competitivos_criado_por_usuario_id",
                table: "eventos_competitivos",
                column: "criado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_competitivos_season_id",
                table: "eventos_competitivos",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_lados_series_capitao_jogador_id",
                table: "lados_series",
                column: "capitao_jogador_id");

            migrationBuilder.CreateIndex(
                name: "ux_lados_series_serie_id_ordem",
                table: "lados_series",
                columns: new[] { "serie_id", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_operacoes_idempotentes_ator_metodo_rota_chave",
                table: "operacoes_idempotentes",
                columns: new[] { "ator_usuario_id", "metodo", "rota", "chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participantes_esperados_series_jogador_id",
                table: "participantes_esperados_series",
                column: "jogador_id");

            migrationBuilder.CreateIndex(
                name: "ix_participantes_esperados_series_lado_serie_id",
                table: "participantes_esperados_series",
                column: "lado_serie_id");

            migrationBuilder.CreateIndex(
                name: "ix_partidas_lado_vencedor_id_serie_id",
                table: "partidas",
                columns: new[] { "lado_vencedor_id", "serie_id" });

            migrationBuilder.CreateIndex(
                name: "ux_partidas_serie_id_ordem",
                table: "partidas",
                columns: new[] { "serie_id", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_picks_partidas_lado_serie_id_serie_id",
                table: "picks_partidas",
                columns: new[] { "lado_serie_id", "serie_id" });

            migrationBuilder.CreateIndex(
                name: "ix_picks_partidas_partida_id_serie_id",
                table: "picks_partidas",
                columns: new[] { "partida_id", "serie_id" });

            migrationBuilder.CreateIndex(
                name: "ux_picks_partidas_champion_valido",
                table: "picks_partidas",
                columns: new[] { "partida_id", "champion_id" },
                unique: true,
                filter: "valido");

            migrationBuilder.CreateIndex(
                name: "ux_picks_partidas_slot_valido",
                table: "picks_partidas",
                columns: new[] { "partida_id", "lado_serie_id", "ordem" },
                unique: true,
                filter: "valido");

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_competitiva_ator_usuario_id",
                table: "registros_auditoria_competitiva",
                column: "ator_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_rodadas_competicao_id",
                table: "rodadas",
                column: "competicao_id");

            migrationBuilder.Sql(
                "ALTER TABLE rodadas ADD CONSTRAINT ux_rodadas_competicao_id_ordem UNIQUE (competicao_id, ordem) DEFERRABLE INITIALLY DEFERRED");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_atualizada_por_usuario_id",
                table: "seasons",
                column: "atualizada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_criada_por_usuario_id",
                table: "seasons",
                column: "criada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_seasons_ano_ordem_no_ano",
                table: "seasons",
                columns: new[] { "ano", "ordem_no_ano" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_seasons_ativa",
                table: "seasons",
                column: "estado",
                unique: true,
                filter: "estado = 'Ativa'");

            migrationBuilder.Sql(
                "ALTER TABLE seasons ADD CONSTRAINT ex_seasons_periodo EXCLUDE USING gist (daterange(data_inicio, data_fim_exclusiva, '[)') WITH &&)");

            migrationBuilder.CreateIndex(
                name: "ix_series_atualizada_por_usuario_id",
                table: "series",
                column: "atualizada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_competicao_id_season_id",
                table: "series",
                columns: new[] { "competicao_id", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_series_criada_por_usuario_id",
                table: "series",
                column: "criada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_draft_montagem_id",
                table: "series",
                column: "draft_montagem_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_evento_id_season_id",
                table: "series",
                columns: new[] { "evento_id", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_series_lado_vencedor_id_id",
                table: "series",
                columns: new[] { "lado_vencedor_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_series_rodada_id_competicao_id",
                table: "series",
                columns: new[] { "rodada_id", "competicao_id" });

            migrationBuilder.CreateIndex(
                name: "ix_series_season_id",
                table: "series",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_versao_regras_id_competicao_id",
                table: "series",
                columns: new[] { "versao_regras_id", "competicao_escopo_id" });

            migrationBuilder.CreateIndex(
                name: "ix_series_versao_regras_id_season_id",
                table: "series",
                columns: new[] { "versao_regras_id", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_versoes_regras_competicao_id_season_id",
                table: "versoes_regras",
                columns: new[] { "competicao_id", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_versoes_regras_publicada_por_usuario_id",
                table: "versoes_regras",
                column: "publicada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_versoes_regras_season_id",
                table: "versoes_regras",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ux_versoes_regras_competicao_numero",
                table: "versoes_regras",
                columns: new[] { "competicao_id", "numero" },
                unique: true,
                filter: "competicao_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_versoes_regras_season_numero_geral",
                table: "versoes_regras",
                columns: new[] { "season_id", "numero" },
                unique: true,
                filter: "competicao_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_lados_series_serie_id",
                table: "lados_series",
                column: "serie_id",
                principalTable: "series",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_competicoes_season_id",
                table: "competicoes");

            migrationBuilder.DropForeignKey(
                name: "fk_eventos_competitivos_season_id",
                table: "eventos_competitivos");

            migrationBuilder.DropForeignKey(
                name: "fk_series_season_id",
                table: "series");

            migrationBuilder.DropForeignKey(
                name: "fk_versoes_regras_season_id",
                table: "versoes_regras");

            migrationBuilder.DropForeignKey(
                name: "fk_series_evento_id",
                table: "series");

            migrationBuilder.DropForeignKey(
                name: "fk_lados_series_serie_id",
                table: "lados_series");

            migrationBuilder.DropTable(
                name: "calendarios_competitivos");

            migrationBuilder.DropTable(
                name: "evento_times");

            migrationBuilder.DropTable(
                name: "operacoes_idempotentes");

            migrationBuilder.DropTable(
                name: "participantes_esperados_series");

            migrationBuilder.DropTable(
                name: "picks_partidas");

            migrationBuilder.DropTable(
                name: "registros_auditoria_competitiva");

            migrationBuilder.DropTable(
                name: "partidas");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "eventos_competitivos");

            migrationBuilder.DropTable(
                name: "series");

            migrationBuilder.DropTable(
                name: "lados_series");

            migrationBuilder.DropTable(
                name: "rodadas");

            migrationBuilder.DropTable(
                name: "versoes_regras");

            migrationBuilder.DropTable(
                name: "competicoes");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "nivel_hierarquico",
                value: 500);
        }
    }
}
