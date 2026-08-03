using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class DefinirOrdemEscolhaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<DefinirOrdemEscolhaDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<DefinirOrdemEscolhaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(DefinirOrdemEscolhaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var modo = Enum.Parse<DraftMontagemOrdemEscolhaModo>(command.Request.Modo, true);
        montagem.DefinirOrdemEscolha(modo, command.Request.CapitaesIds);
        await repository.SaveTeamReorderingAsync(command.Id, cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(
            command.Id,
            DraftMontagemRealtimeStateFactory.Create(updated, DateTimeOffset.UtcNow),
            cancellationToken);
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
