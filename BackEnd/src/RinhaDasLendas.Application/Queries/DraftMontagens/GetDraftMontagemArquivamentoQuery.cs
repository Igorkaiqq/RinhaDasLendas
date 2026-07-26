using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetDraftMontagemArquivamentoQuery(Guid Id) : IRequest<DraftMontagemArquivamentoDto?>;
