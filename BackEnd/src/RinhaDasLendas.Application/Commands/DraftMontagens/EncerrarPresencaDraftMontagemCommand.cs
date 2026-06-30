using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record EncerrarPresencaDraftMontagemCommand(Guid Id, EncerrarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
