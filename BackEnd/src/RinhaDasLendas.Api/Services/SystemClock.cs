using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
