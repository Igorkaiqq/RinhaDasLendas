using MediatR;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

public sealed class ReativarTimeCommandHandler(ITimeRepository timeRepository) : IRequestHandler<ReativarTimeCommand, TimeResponseDto?>
{
    public async Task<TimeResponseDto?> Handle(ReativarTimeCommand command, CancellationToken cancellationToken)
    {
        var time = await timeRepository.GetByIdAsync(command.TimeId, cancellationToken);
        if (time is null)
        {
            return null;
        }

        await TimeHandlerHelpers.EnsureUniqueActiveNameAndTagAsync(timeRepository, time.Nome, time.Tag, time.Id, cancellationToken);
        await TimeHandlerHelpers.EnsureActivePlayersAsync(timeRepository, time.Membros.Select(membro => membro.JogadorId).ToList(), cancellationToken);
        time.Reativar();
        await timeRepository.SaveChangesAsync(cancellationToken);
        return TimeResponseDto.FromEntity(time);
    }
}
