using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Tests.InfrastructureTests;

public sealed class DraftMontagemSaveConflictClassifierTests
{
    [Fact]
    public void Deve_classificar_concorrencia_de_versao()
    {
        DraftMontagemSaveConflictClassifier.Classify(new DbUpdateConcurrencyException())
            .Should().Be(DraftMontagemSaveResultado.ConflitoDeVersao);
    }

    [Theory]
    [InlineData("ix_draft_montagem_presencas_draft_montagem_id_usuario_id")]
    [InlineData("ix_draft_montagem_presencas_draft_montagem_id_jogador_id")]
    public void Deve_classificar_somente_constraints_exatas_de_presenca(string constraintName)
    {
        DraftMontagemSaveConflictClassifier.Classify(UniqueViolation(constraintName))
            .Should().Be(DraftMontagemSaveResultado.ConflitoDePresencaConfirmada);
    }

    [Theory]
    [InlineData("IX_draft_montagem_participantes_draft_montagem_id_jogador_id")]
    [InlineData("IX_draft_montagem_times_draft_montagem_id_ordem")]
    public void Nao_deve_classificar_constraint_estrutural_sem_contexto_de_versao(string constraintName)
    {
        DraftMontagemSaveConflictClassifier.Classify(UniqueViolation(constraintName))
            .Should().BeNull();
        DraftMontagemSaveConflictClassifier.IsStructuralUniqueViolation(UniqueViolation(constraintName))
            .Should().BeTrue();
    }

    [Fact]
    public void Nao_deve_classificar_constraint_diferente()
    {
        DraftMontagemSaveConflictClassifier.Classify(UniqueViolation("ix_usuarios_normalized_user_name"))
            .Should().BeNull();
    }

    private static DbUpdateException UniqueViolation(string constraintName)
    {
        var postgresException = new PostgresException(
            "duplicate key",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            constraintName: constraintName);
        return new DbUpdateException("save failed", postgresException);
    }
}
