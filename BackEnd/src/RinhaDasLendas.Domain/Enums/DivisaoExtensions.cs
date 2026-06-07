namespace RinhaDasLendas.Domain.Enums;

public static class DivisaoExtensions
{
    public static string ToDisplayName(this Divisao divisao)
    {
        return divisao.ToString();
    }

    public static bool TryParseDisplayName(string? value, out Divisao divisao)
    {
        divisao = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim() switch
        {
            "IV" => Set(Divisao.IV, out divisao),
            "III" => Set(Divisao.III, out divisao),
            "II" => Set(Divisao.II, out divisao),
            "I" => Set(Divisao.I, out divisao),
            _ => false
        };
    }

    private static bool Set(Divisao value, out Divisao divisao)
    {
        divisao = value;
        return true;
    }
}
