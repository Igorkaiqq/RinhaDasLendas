using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record ConfirmarPresencaDraftMontagemCommand(Guid Id, ConfirmarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
