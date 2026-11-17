using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgModelTest
{
    private readonly LayananReff _gudangUtama = new("GDU", "Gudang Utama");
    private readonly LayananReff _depoRawatJalan = new("DRJ", "Depo Rawat Jalan");
    private readonly BrgReff _amoxicillin = new("BRG001", "Amoxicillin 500mg");
    private readonly BrgReff _paracetamol = new("BRG002", "Paracetamol 500mg");
    private readonly SatuanType _satuanTablet = new("TAB", "Tablet");
    private readonly SatuanType _satuanBotol = new("BTL", "Botol");
    private readonly DateTime _baseDate = new(2026, 11, 10, 10, 0, 0);

    private MutasiBrgModel CreateSut(string id = "MT2600001A", string orderMutasiId = "ORD001")
    {
        return MutasiBrgModel.Create(
            id,
            _baseDate,
            _gudangUtama,
            _depoRawatJalan,
            orderMutasiId,
            "Pengiriman logistik farmasi mingguan",
            "USR01");
    }

    private MutasiBrgItemModel CreateSampleItem(
        int noUrut = 1,
        BrgReff? brg = null,
        decimal qty = 10m,
        string batch = "BATCH-A1",
        string expDate = "2027-12-31")
    {
        return new MutasiBrgItemModel(
            noUrut,
            brg ?? _amoxicillin,
            batch,
            expDate,
            qty,
            _satuanTablet);
    }

    #region MUTASI_BRG_ITEM_MODEL TESTS

    [Fact]
    public void ItemModel_ValidParameters_ShouldInitializePropertiesCorrectly()
    {
        // Act
        var item = new MutasiBrgItemModel(1, _amoxicillin, "B123", "2027-01-01", 50m, _satuanTablet);

        // Assert
        item.NoUrut.Should().Be(1);
        item.Brg.BrgId.Should().Be("BRG001");
        item.Brg.BrgName.Should().Be("Amoxicillin 500mg");
        item.BatchNo.Should().Be("B123");
        item.ExpiredDate.Should().Be("2027-01-01");
        item.QtyKirim.Should().Be(50m);
        item.Satuan.SatuanId.Should().Be("TAB");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ItemModel_InvalidQtyKirim_ShouldThrowArgumentException(decimal invalidQty)
    {
        // Act
        var act = () => new MutasiBrgItemModel(1, _amoxicillin, "B123", "2027-01-01", invalidQty, _satuanTablet);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyKirim*");
    }

    [Fact]
    public void ItemModel_NullOrInvalidBrg_ShouldThrowArgumentException()
    {
        // Act
        var actNull = () => new MutasiBrgItemModel(1, null!, "B123", "2027-01-01", 10m, _satuanTablet);
        var actEmpty = () => new MutasiBrgItemModel(1, new BrgReff("", ""), "B123", "2027-01-01", 10m, _satuanTablet);

        // Assert
        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ItemModel_NullOrInvalidSatuan_ShouldThrowArgumentException()
    {
        // Act
        var actNull = () => new MutasiBrgItemModel(1, _amoxicillin, "B123", "2027-01-01", 10m, null!);
        var actEmpty = () => new MutasiBrgItemModel(1, _amoxicillin, "B123", "2027-01-01", 10m, new SatuanType("", ""));

        // Assert
        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ItemModel_SetQtyKirim_Valid_ShouldUpdateValue()
    {
        // Arrange
        var item = CreateSampleItem(qty: 10m);

        // Act
        item.SetQtyKirim(25m);

        // Assert
        item.QtyKirim.Should().Be(25m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ItemModel_SetQtyKirim_Invalid_ShouldThrowArgumentException(decimal invalidQty)
    {
        // Arrange
        var item = CreateSampleItem();

        // Act
        var act = () => item.SetQtyKirim(invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyKirim*");
    }

    [Fact]
    public void ItemModel_SetNoUrut_Valid_ShouldUpdateValue()
    {
        // Arrange
        var item = CreateSampleItem(noUrut: 1);

        // Act
        item.SetNoUrut(5);

        // Assert
        item.NoUrut.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ItemModel_SetNoUrut_Invalid_ShouldThrowArgumentException(int invalidNoUrut)
    {
        // Arrange
        var item = CreateSampleItem();

        // Act
        var act = () => item.SetNoUrut(invalidNoUrut);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*NoUrut*");
    }

    [Fact]
    public void ItemModel_SetBatch_ShouldUpdateBatchAndExpiredDate()
    {
        // Arrange
        var item = CreateSampleItem();

        // Act
        item.SetBatch("NEW-BATCH", "2028-06-30");

        // Assert
        item.BatchNo.Should().Be("NEW-BATCH");
        item.ExpiredDate.Should().Be("2028-06-30");
    }

    #endregion

    #region MUTASI_BRG_MODEL CREATION TESTS

    [Fact]
    public void Create_ValidParameters_ShouldInitializeCorrectlyInDraftState()
    {
        // Act
        var sut = CreateSut("MT2600001A", "ORD-123");

        // Assert
        sut.MutasiBrgId.Should().Be("MT2600001A");
        sut.MutasiBrgDate.Should().Be(_baseDate);
        sut.State.Should().Be(MutasiBrgStateEnum.Draft);
        sut.LayananAsal.LayananId.Should().Be("GDU");
        sut.LayananTujuan.LayananId.Should().Be("DRJ");
        sut.OrderMutasiId.Should().Be("ORD-123");
        sut.DispatchNote.Should().Be("Pengiriman logistik farmasi mingguan");
        sut.IssuerUserId.Should().BeEmpty();
        sut.ListItem.Should().BeEmpty();
        sut.AuditTrail.Created.UserId.Should().Be("USR01");
    }

    [Fact]
    public void Create_EmptyId_ShouldAutoGenerate10CharId()
    {
        // Act
        var sut = MutasiBrgModel.Create("", _baseDate, _gudangUtama, _depoRawatJalan);

        // Assert
        sut.MutasiBrgId.Should().NotBeNullOrWhiteSpace();
        sut.MutasiBrgId.Length.Should().Be(10);
        sut.MutasiBrgId.Should().StartWith("MT");
    }

    [Fact]
    public void Create_SameLayananAsalAndTujuan_ShouldThrowArgumentException()
    {
        // Act
        var act = () => MutasiBrgModel.Create(
            "MT2600001A",
            _baseDate,
            _gudangUtama,
            new LayananReff("GDU", "Gudang Utama Duplikat"),
            userId: "USR01");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh sama*");
    }

    [Fact]
    public void Create_NullOrInvalidLayanan_ShouldThrowArgumentException()
    {
        // Act & Assert
        var actNullAsal = () => MutasiBrgModel.Create("MT2600001A", _baseDate, null!, _depoRawatJalan);
        var actNullTujuan = () => MutasiBrgModel.Create("MT2600001A", _baseDate, _gudangUtama, null!);
        var actEmptyAsal = () => MutasiBrgModel.Create("MT2600001A", _baseDate, new LayananReff("", ""), _depoRawatJalan);
        var actEmptyTujuan = () => MutasiBrgModel.Create("MT2600001A", _baseDate, _gudangUtama, new LayananReff("", ""));

        actNullAsal.Should().Throw<ArgumentException>();
        actNullTujuan.Should().Throw<ArgumentException>();
        actEmptyAsal.Should().Throw<ArgumentException>();
        actEmptyTujuan.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ADD_ITEM TESTS

    [Fact]
    public void AddItem_WhenDraft_ShouldAddItemAndAutoAssignNoUrut()
    {
        // Arrange
        var sut = CreateSut();
        var item1 = CreateSampleItem(noUrut: 0, brg: _amoxicillin, qty: 10m);
        var item2 = CreateSampleItem(noUrut: 0, brg: _paracetamol, qty: 20m);

        // Act
        sut.AddItem(item1);
        sut.AddItem(item2);

        // Assert
        sut.ListItem.Should().HaveCount(2);
        item1.NoUrut.Should().Be(1);
        item2.NoUrut.Should().Be(2);
    }

    [Fact]
    public void AddItem_WithDuplicateExplicitNoUrut_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var item1 = CreateSampleItem(noUrut: 1);
        var item2 = CreateSampleItem(noUrut: 1, brg: _paracetamol);

        // Act
        sut.AddItem(item1);
        var act = () => sut.AddItem(item2);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*sudah digunakan*");
    }

    [Fact]
    public void AddItem_WhenNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_DISPATCH");

        var newItem = CreateSampleItem(noUrut: 0, brg: _paracetamol);

        // Act
        var act = () => sut.AddItem(newItem);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan pada status Draft*");
    }

    #endregion

    #region REMOVE_ITEM TESTS

    [Fact]
    public void RemoveItem_WhenDraft_ShouldRemoveItemAndReorderRemaining()
    {
        // Arrange
        var sut = CreateSut();
        var item1 = CreateSampleItem(noUrut: 0, brg: _amoxicillin);
        var item2 = CreateSampleItem(noUrut: 0, brg: _paracetamol);
        var item3 = CreateSampleItem(noUrut: 0, brg: new BrgReff("BRG003", "Vitamin C"));

        sut.AddItem(item1);
        sut.AddItem(item2);
        sut.AddItem(item3);

        // Act - remove middle item (NoUrut 2)
        sut.RemoveItem(2);

        // Assert
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem[0].Brg.BrgId.Should().Be("BRG001");
        sut.ListItem[0].NoUrut.Should().Be(1);
        sut.ListItem[1].Brg.BrgId.Should().Be("BRG003");
        sut.ListItem[1].NoUrut.Should().Be(2);
    }

    [Fact]
    public void RemoveItem_ItemNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());

        // Act
        var act = () => sut.RemoveItem(99);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public void RemoveItem_WhenNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_DISPATCH");

        // Act
        var act = () => sut.RemoveItem(1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan pada status Draft*");
    }

    #endregion

    #region DISPATCH TESTS

    [Fact]
    public void Dispatch_ValidDraftWithItems_ShouldTransitionToDispatched()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        var dispatchTime = new DateTime(2026, 11, 10, 14, 30, 0);

        // Act
        sut.Dispatch("USR_LOGISTIK", dispatchTime);

        // Assert
        sut.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        sut.IssuerUserId.Should().Be("USR_LOGISTIK");
        sut.MutasiBrgDate.Should().Be(dispatchTime);
        sut.AuditTrail.Modified.UserId.Should().Be("USR_LOGISTIK");
    }

    [Fact]
    public void Dispatch_NoItems_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Dispatch("USR_LOGISTIK");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*minimal 1 item*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Dispatch_EmptyIssuerUserId_ShouldThrowArgumentException(string? invalidUserId)
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());

        // Act
        var act = () => sut.Dispatch(invalidUserId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*IssuerUserId*");
    }

    [Fact]
    public void Dispatch_WhenAlreadyDispatched_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_LOGISTIK");

        // Act
        var act = () => sut.Dispatch("ANOTHER_USER");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan pada status Draft*");
    }

    #endregion

    #region CANCEL TESTS

    [Fact]
    public void Cancel_WhenDispatched_ShouldTransitionToCancelled()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_LOGISTIK");

        // Act
        sut.Cancel("USR_SUPERVISOR");

        // Assert
        sut.State.Should().Be(MutasiBrgStateEnum.Cancelled);
        sut.AuditTrail.IsVoided.Should().BeTrue();
        sut.AuditTrail.Voided.UserId.Should().Be("USR_SUPERVISOR");
    }

    [Fact]
    public void Cancel_WhenStillDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());

        // Act
        var act = () => sut.Cancel("USR_SUPERVISOR");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pembatalan hanya diizinkan jika status Dispatched*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_LOGISTIK");
        sut.Cancel("USR_SUPERVISOR");

        // Act
        var act = () => sut.Cancel("ANOTHER_SUPERVISOR");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pembatalan hanya diizinkan jika status Dispatched*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cancel_EmptyUserId_ShouldThrowArgumentException(string? invalidUserId)
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR_LOGISTIK");

        // Act
        var act = () => sut.Cancel(invalidUserId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId pembatal*");
    }

    #endregion

    #region LOAD AND KEY TESTS

    [Fact]
    public void Load_ShouldReconstructModelExactly()
    {
        // Arrange
        var auditTrail = AuditTrailType.Create("USR01", _baseDate);
        var items = new List<MutasiBrgItemModel>
        {
            CreateSampleItem(1, _amoxicillin, 10m, "BATCH-01", "2027-01-01"),
            CreateSampleItem(2, _paracetamol, 25m, "BATCH-02", "2027-06-30")
        };

        // Act
        var model = MutasiBrgModel.Load(
            "MT2600001A",
            _baseDate,
            MutasiBrgStateEnum.Dispatched,
            _gudangUtama,
            _depoRawatJalan,
            "ORD-PERSISTED",
            "Catatan persisten",
            "ISSUER_PERSISTED",
            auditTrail,
            items);

        // Assert
        model.MutasiBrgId.Should().Be("MT2600001A");
        model.MutasiBrgDate.Should().Be(_baseDate);
        model.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        model.LayananAsal.LayananId.Should().Be("GDU");
        model.LayananTujuan.LayananId.Should().Be("DRJ");
        model.OrderMutasiId.Should().Be("ORD-PERSISTED");
        model.DispatchNote.Should().Be("Catatan persisten");
        model.IssuerUserId.Should().Be("ISSUER_PERSISTED");
        model.ListItem.Should().HaveCount(2);
    }

    [Fact]
    public void Key_ShouldReturnValidIMutasiBrgKey()
    {
        // Act
        var key = MutasiBrgModel.Key("MT2600001A");

        // Assert
        key.MutasiBrgId.Should().Be("MT2600001A");
    }

    [Fact]
    public void UpdateDispatchNote_WhenDraft_ShouldUpdateNote()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.UpdateDispatchNote("Catatan baru");

        // Assert
        sut.DispatchNote.Should().Be("Catatan baru");
    }

    [Fact]
    public void UpdateDispatchNote_WhenDispatched_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR01");

        // Act
        var act = () => sut.UpdateDispatchNote("Catatan baru");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetOrderMutasiId_WhenDraft_ShouldUpdateId()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetOrderMutasiId("ORD-999");

        // Assert
        sut.OrderMutasiId.Should().Be("ORD-999");
    }

    [Fact]
    public void SetOrderMutasiId_WhenDispatched_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem(CreateSampleItem());
        sut.Dispatch("USR01");

        // Act
        var act = () => sut.SetOrderMutasiId("ORD-999");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
