using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RepublicarCancelamentoDraftArquivadoCommand(Guid Id)
    : IRequest<DraftMontagemArquivamentoResultadoDto?>;
