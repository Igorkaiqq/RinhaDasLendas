using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimePublisher publisher)
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
        foreach (var expirado in expirados)
        {
            await publisher.PublishAfterCommitAsync(expirado.Id, DraftMontagemSnapshotScope.IncludingArchived);
        }
        var result = await repository.TryClaimPublicacaoDiscordAsync(
            command.Id,
            tipo,
            Guid.NewGuid(),
            agora.Add(ClaimDuration),
            agora,
            cancellationToken);
        if (result?.VersionStamp is not null)
        {
            await publisher.PublishAfterCommitAsync(
                result.VersionStamp.Id,
                tipo == DraftMontagemPublicacaoDiscordTipo.Cancelamento
                    ? DraftMontagemSnapshotScope.IncludingArchived
                    : DraftMontagemSnapshotScope.Active);
        }

        return result is null ? null : ClaimPublicacaoDiscordResponseDto.FromModel(result.Claim);
    }
}
