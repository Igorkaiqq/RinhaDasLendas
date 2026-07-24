using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Rules;

public static class AgendamentoPresencaRules
{
    public const int MinimumNameLength = 3;
    public const int MaximumNameLength = 100;
    public const int MaximumObservationLength = 500;

    public static string NormalizeName(string? name) => name?.Trim() ?? string.Empty;

    public static string? NormalizeObservation(string? observation) =>
        string.IsNullOrWhiteSpace(observation) ? null : observation.Trim();

    public static bool HasValidNameLength(string name) =>
        name.Length is >= MinimumNameLength and <= MaximumNameLength;

    public static bool HasValidObservationLength(string? observation) =>
        observation is null || observation.Length <= MaximumObservationLength;

    public static bool HasValidDays(IReadOnlyCollection<DiaSemanaIso>? days) =>
        days is { Count: > 0 }
        && days.All(day => (int)day is >= (int)DiaSemanaIso.Segunda and <= (int)DiaSemanaIso.Domingo);

    public static bool HasUniqueDays(IReadOnlyCollection<DiaSemanaIso>? days) =>
        days is null || days.Distinct().Count() == days.Count;

    public static bool HasValidTimeRange(TimeOnly publication, TimeOnly closure) =>
        HasMinutePrecision(publication)
        && HasMinutePrecision(closure)
        && closure > publication;

    private static bool HasMinutePrecision(TimeOnly time) => time.Ticks % TimeSpan.TicksPerMinute == 0;
}
