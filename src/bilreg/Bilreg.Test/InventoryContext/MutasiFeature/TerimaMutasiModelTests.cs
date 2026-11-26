using System.Reflection;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class TerimaMutasiModelTests
{
    private readonly LayananReff _gudangUtama = new("GDU", "Gudang Utama");
    private readonly LayananReff _depoRawatJalan = new("DRJ", "Depo Rawat Jalan");
    private readonly BrgReff _amoxicillin = new("BRG001", "Amoxicillin 500mg");
    private readonly BrgReff _paracetamol = new("BRG002", "Paracetamol 500mg");
    private readonly SatuanType _satuanTablet = new("TAB", "Tablet");
    private readonly DateTime _baseDate = new(2026, 11, 25, 10, 0, 0);

    private TerimaMutasiItemModel CreateItem(
        int noUrut = 1,
        BrgReff? brg = null,
        decimal qtyTerima = 10m)
    {
        return new TerimaMutasiItemModel(
            noUrut,
            brg ?? _amoxicillin,
            "BATCH-A1",
            "2027-12-31",
            qtyTerima,
            _satuanTablet);
    }

    #region ENUM & ITEM MODEL

    [Fact]
    public void TerimaMutasiStateEnum_ShouldOnlyContainReceived()
    {
        Enum.GetValues<TerimaMutasiStateEnum>().Should().ContainSingle()
            .Which.Should().Be(TerimaMutasiStateEnum.Received);
        ((int)TerimaMutasiStateEnum.Received).Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void ItemModel_WhenQtyTerimaZeroOrNegative_ShouldThrowArgumentException(decimal invalidQty)
    {
        var act = () => new TerimaMutasiItemModel(
            1, _amoxicillin, "B1", "2027-12-31", invalidQty, _satuanTablet);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyTerima*");
    }

    [Fact]
    public void ItemModel_CreateConfirmed_ShouldInitializePropertiesCorrectly()
    {
        var item = TerimaMutasiItemModel.CreateConfirmed(
            1, _amoxicillin, " BATCH-1 ", " 2027-12-31 ", 15m, _satuanTablet);

        item.NoUrut.Should().Be(1);
        item.Brg.Should().Be(_amoxicillin);
        item.BatchNo.Should().Be("BATCH-1");
        item.ExpiredDate.Should().Be("2027-12-31");
        item.QtyTerima.Should().Be(15m);
        item.Satuan.Should().Be(_satuanTablet);
    }

    #endregion

    #region CREATE CONFIRMED - POSITIVE

    [Fact]
    public void CreateConfirmed_WhenValid_ShouldSetStateReceivedAndGenerateValidId()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(1, _amoxicillin, qtyTerima: 10m),
            CreateItem(2, _paracetamol, qtyTerima: 20m)
        };

        // Act
        var sut = TerimaMutasiModel.CreateConfirmed(
            mutasiBrgId: "MT2600001A",
            orderMutasiId: "ORD001",
            layananAsal: _gudangUtama,
            layananTujuan: _depoRawatJalan,
            receiverUserId: "USR-REC-01",
            listItem: items,
            note: "Barang diterima lengkap",
            receiveDate: _baseDate);

        // Assert
        sut.TerimaMutasiId.Should().NotBeNullOrWhiteSpace();
        sut.TerimaMutasiId.Should().StartWith("TM");
        sut.TerimaMutasiId.Length.Should().BeLessThanOrEqualTo(10);
        sut.State.Should().Be(TerimaMutasiStateEnum.Received);
        sut.MutasiBrgId.Should().Be("MT2600001A");
        sut.OrderMutasiId.Should().Be("ORD001");
        sut.LayananAsal.Should().Be(_gudangUtama);
        sut.LayananTujuan.Should().Be(_depoRawatJalan);
        sut.ReceiverUserId.Should().Be("USR-REC-01");
        sut.TerimaMutasiDate.Should().Be(_baseDate);
        sut.Note.Should().Be("Barang diterima lengkap");
        sut.AuditTrail.Created.UserId.Should().Be("USR-REC-01");
        sut.AuditTrail.Created.Timestamp.Should().Be(_baseDate);
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem[0].QtyTerima.Should().Be(10m);
        sut.ListItem[1].QtyTerima.Should().Be(20m);
    }

    [Fact]
    public void CreateConfirmed_WhenItemNoUrutIsZero_ShouldAssignSequentialNoUrut()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(0, _amoxicillin, qtyTerima: 5m),
            CreateItem(0, _paracetamol, qtyTerima: 8m)
        };

        // Act
        var sut = TerimaMutasiModel.CreateConfirmed(
            "MT2600001A", "", _gudangUtama, _depoRawatJalan, "USR01", items, receiveDate: _baseDate);

        // Assert
        sut.ListItem[0].NoUrut.Should().Be(1);
        sut.ListItem[1].NoUrut.Should().Be(2);
    }

    #endregion

    #region CREATE CONFIRMED - NEGATIVE INVARIANTS

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    public void CreateConfirmed_WhenMutasiBrgIdInvalid_ShouldThrowArgumentException(string invalidMutasiBrgId)
    {
        var items = new[] { CreateItem() };

        var act = () => TerimaMutasiModel.CreateConfirmed(
            invalidMutasiBrgId, "", _gudangUtama, _depoRawatJalan, "USR01", items);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*MutasiBrgId*");
    }

    [Fact]
    public void CreateConfirmed_WhenItemsEmpty_ShouldThrowArgumentException()
    {
        var act = () => TerimaMutasiModel.CreateConfirmed(
            "MT2600001A", "", _gudangUtama, _depoRawatJalan, "USR01", Array.Empty<TerimaMutasiItemModel>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*kosong*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConfirmed_WhenReceiverUserIdInvalid_ShouldThrowArgumentException(string? invalidUserId)
    {
        var items = new[] { CreateItem() };

        var act = () => TerimaMutasiModel.CreateConfirmed(
            "MT2600001A", "", _gudangUtama, _depoRawatJalan, invalidUserId!, items);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region LOAD & IMMUTABILITY

    [Fact]
    public void Load_ShouldRehydrateAggregateWithAllProperties()
    {
        // Arrange
        var audit = AuditTrailType.Create("USR-REC-01", _baseDate);
        var items = new[]
        {
            TerimaMutasiItemModel.Load(1, _amoxicillin, "BATCH-1", "2027-06-01", 10m, _satuanTablet)
        };

        // Act
        var sut = TerimaMutasiModel.Load(
            "TM2600001A",
            _baseDate,
            TerimaMutasiStateEnum.Received,
            "MT2600001A",
            "ORD001",
            _gudangUtama,
            _depoRawatJalan,
            "USR-REC-01",
            "Catatan",
            audit,
            items);

        // Assert
        sut.TerimaMutasiId.Should().Be("TM2600001A");
        sut.State.Should().Be(TerimaMutasiStateEnum.Received);
        sut.MutasiBrgId.Should().Be("MT2600001A");
        sut.OrderMutasiId.Should().Be("ORD001");
        sut.Note.Should().Be("Catatan");
        sut.ListItem.Should().ContainSingle();
        sut.ListItem[0].QtyTerima.Should().Be(10m);
    }

    [Fact]
    public void TerimaMutasiModel_ShouldBeImmutableWithNoMutationOrRejectionMethods()
    {
        // AC-06: Tidak ada metode ubah, hapus, batal, atau tolak pada agregat
        var modelMethods = typeof(TerimaMutasiModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        modelMethods.Should().BeEmpty();

        var staticMethods = typeof(TerimaMutasiModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        staticMethods.Should().NotContain("CreateRejected");

        var itemMethods = typeof(TerimaMutasiItemModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        itemMethods.Should().BeEmpty();
    }

    [Fact]
    public void Key_ShouldReturnITerimaMutasiKeyWithId()
    {
        var key = TerimaMutasiModel.Key("TM2600001A");
        key.TerimaMutasiId.Should().Be("TM2600001A");
    }

    #endregion
}
