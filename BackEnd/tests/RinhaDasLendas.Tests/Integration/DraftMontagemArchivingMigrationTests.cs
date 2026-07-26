using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemArchivingMigrationTests
{
    [Fact]
    public void ModeloEf_DeveConterMetadadosConstraintFkRestritivaEIndicesParciais()
    {
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test")
            .Options;
        using var context = new RinhaDasLendasDbContext(options);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DraftMontagem))!;

        entity.FindProperty("ArquivadoEm")!.GetColumnName().Should().Be("arquivado_em");
        entity.FindProperty("ArquivadoPorUsuarioId")!.GetColumnName().Should().Be("arquivado_por_usuario_id");
        entity.FindProperty("MotivoArquivamento")!.GetMaxLength().Should().Be(500);
        entity.GetCheckConstraints().Should().Contain(constraint => constraint.Name == "ck_draft_montagens_arquivamento");
        entity.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.Properties.Single().Name == "ArquivadoPorUsuarioId"
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        entity.GetIndexes().Select(index => index.GetFilter()).Should().Contain("arquivado_em IS NULL");
        entity.GetIndexes().Select(index => index.GetFilter()).Should().Contain("arquivado_em IS NOT NULL");
    }
}
