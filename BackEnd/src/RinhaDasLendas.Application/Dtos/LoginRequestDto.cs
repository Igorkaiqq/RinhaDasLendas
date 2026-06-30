using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record LoginRequestDto(string Email, string Senha);
