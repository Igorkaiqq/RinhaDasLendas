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

public sealed class AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimeNotifier notifier)
    : IRequestHandler<AdquirirClaimPublicacaoDiscordDraftMontagemCommand, ClaimPublicacaoDiscordResponseDto?>
{
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(5);

    public async Task<ClaimPublicacaoDiscordResponseDto?> Handle(
        AdquirirClaimPublicacaoDiscordDraftMontagemCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        if (!DraftMontagemPublicacaoDiscordTipoParser.TryParse(command.Request.Tipo, out var tipo))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }
        var agora = DateTimeOffset.UtcNow;
        var expirados = await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(agora, cancellationToken);
        await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
            expirados,
            repository,
            notifier,
            cancellationToken);
        var claim = await repository.TryClaimPublicacaoDiscordAsync(
            command.Id,
            tipo,
            Guid.NewGuid(),
            agora.Add(ClaimDuration),
            agora,
            cancellationToken);
        if (claim?.Adquirido == true)
        {
            await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
                [command.Id],
                repository,
                notifier,
                cancellationToken);
        }

        return claim is null ? null : ClaimPublicacaoDiscordResponseDto.FromModel(claim);
    }
}
