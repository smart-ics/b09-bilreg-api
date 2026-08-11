using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLegacyScopeRepoTest
{
    private readonly StockLegacyScopeRepo _sut = new(
        new StokLegacyScopeDal(ConnStringHelper.GetTestEnv()));

    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";

    [Fact]
    public void SaveChanges_InsertLoadAndUpdateWatermark()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        _sut.SaveChanges(scope);

        var loaded = _sut.LoadEntity(scope);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.AlignmentStatus.Should().Be(AlignmentStatusEnum.NotAligned);

        var watermarkAt = new DateTime(2026, 3, 1, 12, 0, 0);
        loaded.Value.MarkAligned(watermarkAt, "BK00000001", watermarkAt);
        _sut.SaveChanges(loaded.Value);

        var updated = _sut.LoadEntity(scope).Value;
        updated.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        updated.TglMutasiLast.Should().Be(watermarkAt);
        updated.LastLegacyBukuId.Should().Be("BK00000001");
    }
}
