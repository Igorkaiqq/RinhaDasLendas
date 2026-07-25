using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application;

internal static class DraftMontagemPublicacaoDiscordTipoParser
{
    public static bool TryParse(string? value, out DraftMontagemPublicacaoDiscordTipo tipo)
    {
        tipo = default;
        if (value is null || !Enum.GetNames<DraftMontagemPublicacaoDiscordTipo>()
            .Any(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return Enum.TryParse(value, true, out tipo);
    }
}
