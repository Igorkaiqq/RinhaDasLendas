using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RepublicarCancelamentoDraftArquivadoCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimePublisher publisher)
    : IRequestHandler<RepublicarCancelamentoDraftArquivadoCommand, DraftMontagemArquivamentoResultadoDto?>
{
    public async Task<DraftMontagemArquivamentoResultadoDto?> Handle(RepublicarCancelamentoDraftArquivadoCommand command, CancellationToken cancellationToken)
    {
        var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdIncludingArchivedAsync(command.Id, cancellationToken);
        if (montagem is null
            || montagem.AcoesAdministrativas.All(item => item.Tipo != "CancelamentoPorArquivamento")
            || montagem.PublicacoesDiscord.All(item => item.Tipo != DraftMontagemPublicacaoDiscordTipo.Cancelamento))
        {
            return null;
        }

        var stamp = montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Cancelamento, userId, null, DateTimeOffset.UtcNow);
        if (stamp is null)
        {
            return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
        }

        await repository.SaveChangesAsync(cancellationToken);
        await publisher.PublishAfterCommitAsync(stamp.Id, DraftMontagemSnapshotScope.IncludingArchived);
        return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
    }
}
