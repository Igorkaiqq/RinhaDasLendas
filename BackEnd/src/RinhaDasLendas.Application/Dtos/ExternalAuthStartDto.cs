using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ExternalAuthStartDto(string AuthorizationUrl);
