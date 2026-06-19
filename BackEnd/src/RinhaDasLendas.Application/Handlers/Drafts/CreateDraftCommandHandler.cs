using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Drafts;

public sealed class CreateDraftCommandHandler(
    IDraftRepository draftRepository,
    IValidator<CreateDraftRequestDto> validator) : IRequestHandler<CreateDraftCommand, DraftResponseDto>
{
    public async Task<DraftResponseDto> Handle(CreateDraftCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var jogadoresIds = command.Request.JogadoresIds.ToList();
        var jogadores = await draftRepository.GetJogadoresByIdsAsync(jogadoresIds, cancellationToken);
        DraftHandlerHelpers.EnsureActivePlayers(jogadores, jogadoresIds);

        var (capitaoA, capitaoB) = DraftHandlerHelpers.ResolveCaptainIds(new CreateDraftRequestDtoAdapter(
            command.Request.SortearCapitaes,
            command.Request.CapitaoTimeAId,
            command.Request.CapitaoTimeBId,
            jogadoresIds));
        var primeiroTime = DraftHandlerHelpers.ResolveFirstPick(command.Request.SortearPrimeiroPick, command.Request.PrimeiroTime);

        var draft = new DraftSessao(
            command.Request.Nome,
            command.Request.Observacoes,
            command.Request.TamanhoTime,
            capitaoA,
            capitaoB,
            command.Request.SortearCapitaes ? DraftCriterioSelecao.Sorteio : DraftCriterioSelecao.Manual,
            primeiroTime,
            command.Request.SortearPrimeiroPick ? DraftCriterioSelecao.Sorteio : DraftCriterioSelecao.Manual,
            jogadoresIds);

        await draftRepository.AddAsync(draft, cancellationToken);
        await draftRepository.SaveChangesAsync(cancellationToken);

        var created = await draftRepository.GetByIdAsync(draft.Id, cancellationToken) ?? draft;
        return DraftResponseDto.FromEntity(created);
    }
}
