namespace RinhaDasLendas.Application.Interfaces;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
