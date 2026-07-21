using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemAcaoAdministrativa
{
    private DraftMontagemAcaoAdministrativa()
    {
    }

    public DraftMontagemAcaoAdministrativa(string tipo, Guid responsavelUsuarioId, string? motivo, Guid? jogadorAlvoId = null)
    {
        Id = Guid.NewGuid();
        Tipo = string.IsNullOrWhiteSpace(tipo) ? throw new ArgumentException(MessageCodes.FieldRequired, nameof(tipo)) : tipo.Trim();
        ResponsavelUsuarioId = responsavelUsuarioId;
        JogadorAlvoId = jogadorAlvoId;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        RegistradoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public Guid ResponsavelUsuarioId { get; private set; }
    public Guid? JogadorAlvoId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
}
