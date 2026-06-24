namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemSubstituicao
{
    private DraftMontagemSubstituicao()
    {
    }

    public DraftMontagemSubstituicao(Guid timeId, Guid jogadorSaiuId, Guid reservaEntrouId, string? motivo, Guid responsavelUsuarioId, DateTimeOffset registradoEm)
    {
        Id = Guid.NewGuid();
        TimeId = timeId;
        JogadorSaiuId = jogadorSaiuId;
        ReservaEntrouId = reservaEntrouId;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        ResponsavelUsuarioId = responsavelUsuarioId;
        RegistradoEm = registradoEm;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public Guid TimeId { get; private set; }
    public Guid JogadorSaiuId { get; private set; }
    public Guid ReservaEntrouId { get; private set; }
    public string? Motivo { get; private set; }
    public Guid ResponsavelUsuarioId { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
    public Jogador? JogadorSaiu { get; private set; }
    public Jogador? ReservaEntrou { get; private set; }
}
