using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RepublicarPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<RepublicarPublicacaoDiscordDraftMontagemRequestDto> validator,
    ICurrentUser currentUser,
    IDraftMontagemMetrics metrics,
    IDraftMontagemRealtimePublisher publisher) : IRequestHandler<RepublicarPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RepublicarPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var currentUserId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var stamp = montagem.SolicitarRepublicacaoDiscord(command.Request.Tipo, currentUserId, command.Request.Motivo, DateTimeOffset.UtcNow);
        if (stamp is null)
        {
            return DraftMontagemResponseDto.FromEntity(montagem);
        }

        await repository.SaveChangesAsync(cancellationToken);
        await publisher.PublishAfterCommitAsync(stamp.Id);
        var updated = await repository.ReloadByIdAsync(command.Id, cancellationToken) ?? montagem;
        metrics.RecordDiscordPublication(command.Id, command.Request.Tipo.ToString(), DraftMontagemPublicacaoDiscordStatus.Pendente.ToString());
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
