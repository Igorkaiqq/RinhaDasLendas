using FluentAssertions;
using RinhaDasLendas.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RinhaDasLendas.Tests.Domain;

public sealed class CompetitiveEnumTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static TheoryData<string, (string Name, int Value)[]> StableEnums => new()
    {
        { "SeasonEstado", [("Planejada", 0), ("Ativa", 1), ("Encerrada", 2)] },
        { "SerieTipo", [("DiariaTemporaria", 0), ("ConfrontoOficial", 1), ("Amistoso", 2)] },
        { "SerieEstado", [("Agendada", 0), ("EmAndamento", 1), ("Concluida", 2), ("Cancelada", 3), ("Anulada", 4)] },
        { "SerieFormato", [("Md3", 0), ("Md5", 1)] },
        { "ModoDraft", [("Padrao", 0), ("Fearless", 1)] },
        { "LadoSerieTipo", [("Temporario", 0), ("TimeOficial", 1)] },
        { "PartidaEstado", [("Rascunho", 0), ("Confirmada", 1), ("Remake", 2), ("Anulada", 3)] },
        { "DecisaoPicksRemake", [("PreservarPicks", 0), ("DesconsiderarPicks", 1)] },
        { "MotivoTerminoPartida", [("Normal", 0), ("Surrender", 1)] },
    };

    [Theory]
    [MemberData(nameof(StableEnums))]
    public void Deve_preservar_nomes_e_valores_estaveis(string enumName, (string Name, int Value)[] expected)
    {
        var enumType = typeof(DraftStatus).Assembly.GetType($"RinhaDasLendas.Domain.Enums.{enumName}");

        enumType.Should().NotBeNull().And.Match<Type>(type => type.IsEnum);

        var actual = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => (Name: value.ToString()!, Value: Convert.ToInt32(value)));

        actual.Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(StableEnums))]
    public void Deve_serializar_cada_membro_como_string(string enumName, (string Name, int Value)[] expected)
    {
        var enumType = typeof(DraftStatus).Assembly.GetType($"RinhaDasLendas.Domain.Enums.{enumName}");

        enumType.Should().NotBeNull().And.Match<Type>(type => type.IsEnum);

        foreach (var member in expected)
        {
            var value = Enum.ToObject(enumType!, member.Value);

            JsonSerializer.Serialize(value, enumType!, JsonOptions).Should().Be($"\"{member.Name}\"");
        }
    }
}
