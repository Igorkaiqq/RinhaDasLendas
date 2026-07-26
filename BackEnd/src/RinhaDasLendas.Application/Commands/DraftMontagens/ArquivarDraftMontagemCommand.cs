using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record ArquivarDraftMontagemCommand(Guid Id, ArquivarDraftMontagemRequestDto Request)
    : IRequest<DraftMontagemArquivamentoResultadoDto?>;
