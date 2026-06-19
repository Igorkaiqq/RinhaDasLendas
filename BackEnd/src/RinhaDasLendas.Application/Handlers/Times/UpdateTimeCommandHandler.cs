using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

public sealed class UpdateTimeCommandHandler(
    ITimeRepository timeRepository,
    IValidator<UpdateTimeRequestDto> validator) : IRequestHandler<UpdateTimeCommand, TimeResponseDto?>
{
    public async Task<TimeResponseDto?> Handle(UpdateTimeCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);

        var time = await timeRepository.GetByIdAsync(command.TimeId, cancellationToken);
        if (time is null)
        {
            return null;
        }

        await TimeHandlerHelpers.EnsureUniqueActiveNameAndTagAsync(
            timeRepository,
            command.Request.Nome,
            command.Request.Tag,
            command.TimeId,
            cancellationToken);
        await TimeHandlerHelpers.EnsureActivePlayersAsync(timeRepository, command.Request.JogadoresIds, cancellationToken);

        time.Atualizar(
            command.Request.Nome,
            command.Request.Tag,
            command.Request.Observacoes,
            command.Request.JogadoresIds,
            command.Request.CapitaoId);

        await timeRepository.SaveChangesAsync(cancellationToken);
        return TimeResponseDto.FromEntity(time);
    }
}
