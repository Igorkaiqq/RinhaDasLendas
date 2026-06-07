using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Jogadores;

public sealed record UpdateJogadorCommand(Guid JogadorId, JogadorUpdateRequestDto Request) : IRequest<JogadorResponseDto?>;
