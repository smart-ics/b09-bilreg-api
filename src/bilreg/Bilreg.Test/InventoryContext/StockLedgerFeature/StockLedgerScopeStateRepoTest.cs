using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLedgerScopeStateRepoTest
{
    private readonly StockLedgerScopeStateRepo _sut = new(
        new StockLedgerScopeDal(ConnStringHelper.GetTestEnv()));

    [Fact]
    public void SaveAndLoad_NotReconstructed_RoundTrips()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var model = StockLedgerScopeStateModel.CreateNotReconstructed(
            StockLedgerScopeKeyType.Create("BRGS603", "DO6003"));

        _sut.SaveChanges(model);

        var loaded = _sut.LoadEntity(model);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.NotReconstructed);
        loaded.Value.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        loaded.Value.HasSynchronizationPosition.Should().BeFalse();
        loaded.Value.ReconstructionBasisVersion.Should().BeNull();
        loaded.Value.InconsistencyReason.Should().BeNull();
    }

    [Fact]
    public void SaveAndLoad_OpaqueSynchronizationPosition_RoundTrips()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var position = SynchronizationPositionType.Create(
            [0x0A, 0x1B, 0x2C, 0x3D],
            "fingerprint-v1");

        var model = StockLedgerScopeStateModel
            .CreateNotReconstructed(StockLedgerScopeKeyType.Create("BRGS604", "DO6004"))
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(position, reconstructionBasisVersion: "basis-1");

        _sut.SaveChanges(model);

        var loaded = _sut.LoadEntity(model).Value;
        loaded.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        loaded.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        loaded.HasSynchronizationPosition.Should().BeTrue();
        loaded.SynchronizationPosition.Should().Be(position);
        loaded.ReconstructionBasisVersion.Should().Be("basis-1");
    }

    [Fact]
    public void SaveChanges_UpdateExisting_PersistsTransition()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var initial = StockLedgerScopeStateModel.CreateNotReconstructed(
            StockLedgerScopeKeyType.Create("BRGS605", "DO6005"));
        _sut.SaveChanges(initial);

        var required = _sut.LoadEntity(initial).Value.RequireReconstruction();
        _sut.SaveChanges(required);

        var loaded = _sut.LoadEntity(initial).Value;
        loaded.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.ReconstructionRequired);
    }
}
