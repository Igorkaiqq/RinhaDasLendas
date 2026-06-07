using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RinhaDasLendas.Infrastructure.Persistence;

#nullable disable

namespace RinhaDasLendas.Infrastructure.Migrations;

[DbContext(typeof(RinhaDasLendasDbContext))]
[Migration("20260607000200_SepararEloDivisaoEnums")]
public partial class SepararEloDivisaoEnums : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "divisao",
            table: "jogadores",
            type: "character varying(5)",
            maxLength: 5,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET
                divisao = CASE split_part(elo, ' ', 2)
                    WHEN '4' THEN 'IV'
                    WHEN '3' THEN 'III'
                    WHEN '2' THEN 'II'
                    WHEN '1' THEN 'I'
                    ELSE split_part(elo, ' ', 2)
                END,
                elo = split_part(elo, ' ', 1)
            WHERE elo IN (
                'Ferro IV', 'Ferro III', 'Ferro II', 'Ferro I',
                'Bronze IV', 'Bronze III', 'Bronze II', 'Bronze I',
                'Prata IV', 'Prata III', 'Prata II', 'Prata I',
                'Ouro IV', 'Ouro III', 'Ouro II', 'Ouro I',
                'Platina IV', 'Platina III', 'Platina II', 'Platina I',
                'Esmeralda IV', 'Esmeralda III', 'Esmeralda II', 'Esmeralda I',
                'Diamante IV', 'Diamante III', 'Diamante II', 'Diamante I',
                'Ferro 4', 'Ferro 3', 'Ferro 2', 'Ferro 1',
                'Bronze 4', 'Bronze 3', 'Bronze 2', 'Bronze 1',
                'Prata 4', 'Prata 3', 'Prata 2', 'Prata 1',
                'Ouro 4', 'Ouro 3', 'Ouro 2', 'Ouro 1',
                'Platina 4', 'Platina 3', 'Platina 2', 'Platina 1',
                'Esmeralda 4', 'Esmeralda 3', 'Esmeralda 2', 'Esmeralda 1',
                'Diamante 4', 'Diamante 3', 'Diamante 2', 'Diamante 1'
            );
            """);

        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET elo = 'GraoMestre'
            WHERE elo = 'Grão-Mestre';
            """);

        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET elo = 'Ferro', divisao = 'IV'
            WHERE elo IS NULL OR btrim(elo) = '';
            """);

        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET divisao = 'IV'
            WHERE elo IN ('Ferro', 'Bronze', 'Prata', 'Ouro', 'Platina', 'Esmeralda', 'Diamante')
              AND divisao IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "elo",
            table: "jogadores",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(60)",
            oldMaxLength: 60,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET elo = elo || ' ' || divisao
            WHERE divisao IS NOT NULL;
            """);

        migrationBuilder.Sql(
            """
            UPDATE jogadores
            SET elo = 'Grão-Mestre'
            WHERE elo = 'GraoMestre';
            """);

        migrationBuilder.DropColumn(
            name: "divisao",
            table: "jogadores");

        migrationBuilder.AlterColumn<string>(
            name: "elo",
            table: "jogadores",
            type: "character varying(60)",
            maxLength: 60,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30);
    }
}
