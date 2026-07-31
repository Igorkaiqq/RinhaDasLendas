using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemAcaoAdministrativa
{
    private DraftMontagemAcaoAdministrativa()
    {
    }

    public DraftMontagemAcaoAdministrativa(string tipo, Guid responsavelUsuarioId, string? motivo, Guid? jogadorAlvoId = null)
        : this(tipo, DraftMontagemActor.User(responsavelUsuarioId), motivo, jogadorAlvoId, DateTimeOffset.UtcNow)
    {
    }

    public DraftMontagemAcaoAdministrativa(
        string tipo,
        Guid responsavelUsuarioId,
        string? motivo,
        Guid? jogadorAlvoId,
        DateTimeOffset registradoEm)
        : this(tipo, DraftMontagemActor.User(responsavelUsuarioId), motivo, jogadorAlvoId, registradoEm)
    {
    }

    public DraftMontagemAcaoAdministrativa(
        string tipo,
        DraftMontagemActor responsavel,
        string? motivo,
        Guid? jogadorAlvoId = null)
        : this(tipo, responsavel, motivo, jogadorAlvoId, DateTimeOffset.UtcNow)
    {
    }

    public DraftMontagemAcaoAdministrativa(
        string tipo,
        DraftMontagemActor responsavel,
        string? motivo,
        Guid? jogadorAlvoId,
        DateTimeOffset registradoEm)
    {
        ArgumentNullException.ThrowIfNull(responsavel);
        Id = Guid.NewGuid();
        Tipo = string.IsNullOrWhiteSpace(tipo) ? throw new ArgumentException(MessageCodes.FieldRequired, nameof(tipo)) : tipo.Trim();
        ResponsavelTipo = responsavel.Tipo;
        ResponsavelUsuarioId = responsavel.UsuarioId;
        JogadorAlvoId = jogadorAlvoId;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        RegistradoEm = registradoEm;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public DraftMontagemActorType ResponsavelTipo { get; private set; } = DraftMontagemActorType.User;
    public Guid? ResponsavelUsuarioId { get; private set; }
    public Guid? JogadorAlvoId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
}
