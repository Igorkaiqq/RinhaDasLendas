namespace RinhaDasLendas.Domain.Enums;

public static class EloExtensions
{
    public static bool ExigeDivisao(this Elo elo)
    {
        return elo is Elo.Ferro
            or Elo.Bronze
            or Elo.Prata
            or Elo.Ouro
            or Elo.Platina
            or Elo.Esmeralda
            or Elo.Diamante;
    }

    public static string ToDisplayName(this Elo elo)
    {
        return elo switch
        {
            Elo.GraoMestre => "Grão-Mestre",
            _ => elo.ToString()
        };
    }

    public static bool TryParseDisplayName(string? value, out Elo elo)
    {
        elo = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim() switch
        {
            "Ferro" => Set(Elo.Ferro, out elo),
            "Bronze" => Set(Elo.Bronze, out elo),
            "Prata" => Set(Elo.Prata, out elo),
            "Ouro" => Set(Elo.Ouro, out elo),
            "Platina" => Set(Elo.Platina, out elo),
            "Esmeralda" => Set(Elo.Esmeralda, out elo),
            "Diamante" => Set(Elo.Diamante, out elo),
            "Mestre" => Set(Elo.Mestre, out elo),
            "GraoMestre" or "Grão-Mestre" => Set(Elo.GraoMestre, out elo),
            "Desafiante" => Set(Elo.Desafiante, out elo),
            _ => false
        };
    }

    private static bool Set(Elo value, out Elo elo)
    {
        elo = value;
        return true;
    }
}
