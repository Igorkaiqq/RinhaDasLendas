using MediatR;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record EncerrarPresencaDraftMontagemAutomaticamenteCommand(Guid Id) : IRequest;
