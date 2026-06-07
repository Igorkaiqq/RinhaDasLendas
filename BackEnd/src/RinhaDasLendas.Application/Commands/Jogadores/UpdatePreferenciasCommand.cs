using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Jogadores;

public sealed record UpdatePreferenciasCommand(Guid JogadorId, UpdatePreferenciasRotasRequestDto Request) : IRequest<JogadorResponseDto?>;
