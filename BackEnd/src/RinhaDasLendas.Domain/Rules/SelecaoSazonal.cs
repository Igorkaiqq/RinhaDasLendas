using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Rules;

public enum SelecaoSazonalTipo
{
    Padrao = 0,
    Especifica = 1,
    Todas = 2
}

public sealed class SelecaoSazonal : IEquatable<SelecaoSazonal>
{
    private SelecaoSazonal(SelecaoSazonalTipo tipo, IReadOnlyCollection<Guid> seasonIds)
    {
        Tipo = tipo;
        SeasonIds = seasonIds;
    }

    public SelecaoSazonalTipo Tipo { get; }
    public IReadOnlyCollection<Guid> SeasonIds { get; }

    public static SelecaoSazonal Padrao() =>
        new(SelecaoSazonalTipo.Padrao, Array.Empty<Guid>());

    public static SelecaoSazonal Especifica(IEnumerable<Guid> seasonIds)
    {
        if (seasonIds is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var idsDistintos = seasonIds.Distinct().ToArray();
        if (idsDistintos.Length == 0 || idsDistintos.Contains(Guid.Empty))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return new SelecaoSazonal(
            SelecaoSazonalTipo.Especifica,
            Array.AsReadOnly(idsDistintos));
    }

    public static SelecaoSazonal Todas() =>
        new(SelecaoSazonalTipo.Todas, Array.Empty<Guid>());

    public static SelecaoSazonal Criar(IEnumerable<Guid>? seasonIds, bool todas)
    {
        var idsDistintos = seasonIds?.Distinct().ToArray() ?? [];
        if (todas && idsDistintos.Length > 0)
        {
            throw new DomainException(MessageCodes.SeasonalFilterConflict);
        }

        if (todas)
        {
            return Todas();
        }

        return idsDistintos.Length == 0 ? Padrao() : Especifica(idsDistintos);
    }

    public bool Equals(SelecaoSazonal? other) =>
        other is not null
        && Tipo == other.Tipo
        && SeasonIds.Count == other.SeasonIds.Count
        && SeasonIds.ToHashSet().SetEquals(other.SeasonIds);

    public override bool Equals(object? obj) => obj is SelecaoSazonal other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tipo);
        foreach (var seasonId in SeasonIds.Order())
        {
            hash.Add(seasonId);
        }

        return hash.ToHashCode();
    }
}
