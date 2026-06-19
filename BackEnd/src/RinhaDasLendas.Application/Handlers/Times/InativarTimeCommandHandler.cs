using MediatR;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

public sealed class InativarTimeCommandHandler(ITimeRepository timeRepository) : IRequestHandler<InativarTimeCommand, TimeResponseDto?>
{
    public async Task<TimeResponseDto?> Handle(InativarTimeCommand command, CancellationToken cancellationToken)
    {
        var time = await timeRepository.GetByIdAsync(command.TimeId, cancellationToken);
        if (time is null)
        {
            return null;
        }

        time.Inativar();
        await timeRepository.SaveChangesAsync(cancellationToken);
        return TimeResponseDto.FromEntity(time);
    }
}
