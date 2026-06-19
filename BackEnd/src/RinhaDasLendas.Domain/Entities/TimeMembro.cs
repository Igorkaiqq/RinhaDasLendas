namespace RinhaDasLendas.Domain.Entities;

public sealed class TimeMembro
{
    private TimeMembro()
    {
    }

    public TimeMembro(Guid jogadorId, bool principal = true)
    {
        Id = Guid.NewGuid();
        JogadorId = jogadorId;
        Principal = principal;
        DataCadastro = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TimeId { get; private set; }
    public Guid JogadorId { get; private set; }
    public bool Principal { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public Jogador? Jogador { get; private set; }
}
