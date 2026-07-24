namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed class AgendamentoPresencaProcessingOptions
{
    public const string SectionName = "PresenceSchedule";

    public int MaxBlockedPerCycle { get; init; } = 50;
    public int MaxSchedulesPerCycle { get; init; } = 50;
    public int MaxDatesPerSchedulePerCycle { get; init; } = 31;

    public AgendamentoPresencaProcessingOptions Normalize() => new()
    {
        MaxBlockedPerCycle = Math.Clamp(MaxBlockedPerCycle, 1, 1000),
        MaxSchedulesPerCycle = Math.Clamp(MaxSchedulesPerCycle, 1, 1000),
        MaxDatesPerSchedulePerCycle = Math.Clamp(MaxDatesPerSchedulePerCycle, 1, 366)
    };
}
