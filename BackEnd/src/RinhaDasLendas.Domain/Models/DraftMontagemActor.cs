using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Models;

public sealed record DraftMontagemActor
{
    private DraftMontagemActor(DraftMontagemActorType tipo, Guid? usuarioId)
    {
        Tipo = tipo;
        UsuarioId = usuarioId;
    }

    public DraftMontagemActorType Tipo { get; }
    public Guid? UsuarioId { get; }

    public static DraftMontagemActor User(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(MessageCodes.FieldRequired, nameof(usuarioId));
        }

        return new DraftMontagemActor(DraftMontagemActorType.User, usuarioId);
    }

    public static DraftMontagemActor System()
    {
        return new DraftMontagemActor(DraftMontagemActorType.System, null);
    }
}
