using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Rodada
{
    private Rodada()
    {
    }

    public Rodada(Guid competicaoId, string nome, int ordem, DateTimeOffset criadaEm)
    {
        if (competicaoId == Guid.Empty || ordem <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > 80)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        var criadaEmUtc = criadaEm.ToUniversalTime();
        Id = Guid.NewGuid();
        CompeticaoId = competicaoId;
        Nome = nomeNormalizado;
        Ordem = ordem;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
    }

    public Guid Id { get; private set; }
    public Guid CompeticaoId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Ordem { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }

    internal void Reordenar(int ordem, DateTimeOffset atualizadaEm)
    {
        if (ordem <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Ordem = ordem;
        AtualizadaEm = atualizadaEm.ToUniversalTime();
        Versao++;
    }
}
