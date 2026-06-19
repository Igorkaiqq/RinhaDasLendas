using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

public sealed class CreateTimeCommandHandler(
    ITimeRepository timeRepository,
    IValidator<CreateTimeRequestDto> validator) : IRequestHandler<CreateTimeCommand, TimeResponseDto>
{
    public async Task<TimeResponseDto> Handle(CreateTimeCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        await TimeHandlerHelpers.EnsureUniqueActiveNameAndTagAsync(
            timeRepository,
            command.Request.Nome,
            command.Request.Tag,
            ignoredId: null,
            cancellationToken);
        await TimeHandlerHelpers.EnsureActivePlayersAsync(timeRepository, command.Request.JogadoresIds, cancellationToken);

        var time = new Time(
            command.Request.Nome,
            command.Request.Tag,
            command.Request.Observacoes,
            command.Request.JogadoresIds,
            command.Request.CapitaoId);

        await timeRepository.AddAsync(time, cancellationToken);
        await timeRepository.SaveChangesAsync(cancellationToken);

        var created = await timeRepository.GetByIdAsync(time.Id, cancellationToken) ?? time;
        return TimeResponseDto.FromEntity(created);
    }
}
