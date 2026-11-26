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
        decimal qtyKirim = 10m,
        decimal qtyTerimaBaik = 10m,
        decimal qtySelisih = 0m,
        string alasanSelisih = "")
    {
        return new TerimaMutasiItemModel(
            noUrut,
            brg ?? _amoxicillin,
            "BATCH-A1",
            "2027-12-31",
            qtyKirim,
            qtyTerimaBaik,
            qtySelisih,
            _satuanTablet,
            alasanSelisih);
    }

    private MutasiBrgItemModel CreateMutasiItem(
        int noUrut = 1,
        BrgReff? brg = null,
        decimal qtyKirim = 10m)
    {
        return new MutasiBrgItemModel(
            noUrut,
            brg ?? _amoxicillin,
            "BATCH-A1",
            "2027-12-31",
            qtyKirim,
            _satuanTablet);
    }

    #region CREATE CONFIRMED - POSITIVE

    [Fact]
    public void CreateConfirmed_WhenAllItemsReceivedFully_ShouldSetStateReceivedAndGenerateValidId()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(1, _amoxicillin, qtyKirim: 10m, qtyTerimaBaik: 10m, qtySelisih: 0m),
            CreateItem(2, _paracetamol, qtyKirim: 20m, qtyTerimaBaik: 20m, qtySelisih: 0m)
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
        sut.RejectionReason.Should().BeEmpty();
        sut.AuditTrail.Created.UserId.Should().Be("USR-REC-01");
        sut.AuditTrail.Created.Timestamp.Should().Be(_baseDate);
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem[0].QtyKirim.Should().Be(10m);
        sut.ListItem[0].QtyTerimaBaik.Should().Be(10m);
        sut.ListItem[0].QtySelisih.Should().Be(0m);
    }

    [Fact]
    public void CreateConfirmed_WhenAnyItemHasDiscrepancy_ShouldSetStateReceivedWithDiscrepancy()
    {
        // AC-03, AC-05
        // Arrange
        var items = new[]
        {
            CreateItem(1, _amoxicillin, qtyKirim: 10m, qtyTerimaBaik: 8m, qtySelisih: 2m, alasanSelisih: "2 strip rusak saat pengiriman"),
            CreateItem(2, _paracetamol, qtyKirim: 20m, qtyTerimaBaik: 20m, qtySelisih: 0m)
        };

        // Act
        var sut = TerimaMutasiModel.CreateConfirmed(
            mutasiBrgId: "MT2600001A",
            orderMutasiId: "ORD001",
            layananAsal: _gudangUtama,
            layananTujuan: _depoRawatJalan,
            receiverUserId: "USR-REC-01",
            listItem: items,
            receiveDate: _baseDate);

        // Assert
        sut.State.Should().Be(TerimaMutasiStateEnum.ReceivedWithDiscrepancy);
        sut.ListItem[0].QtyKirim.Should().Be(10m);
        sut.ListItem[0].QtyTerimaBaik.Should().Be(8m);
        sut.ListItem[0].QtySelisih.Should().Be(2m);
        sut.ListItem[0].AlasanSelisih.Should().Be("2 strip rusak saat pengiriman");
    }

    [Fact]
    public void CreateConfirmed_WhenItemNoUrutIsZero_ShouldAssignSequentialNoUrut()
    {
        // Arrange
        var items = new[]
        {
            CreateItem(0, _amoxicillin, qtyKirim: 5m, qtyTerimaBaik: 5m, qtySelisih: 0m),
            CreateItem(0, _paracetamol, qtyKirim: 5m, qtyTerimaBaik: 5m, qtySelisih: 0m)
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

    [Fact]
    public void ItemModel_WhenQtyTerimaBaikNegative_ShouldThrowArgumentException()
    {
        // AC-05
        var act = () => new TerimaMutasiItemModel(
            1, _amoxicillin, "B1", "2027-12-31", 10m, -1m, 11m, _satuanTablet, "rusak");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyTerimaBaik*");
    }

    [Fact]
    public void ItemModel_WhenQtySelisihNegative_ShouldThrowArgumentException()
    {
        // AC-05
        var act = () => new TerimaMutasiItemModel(
            1, _amoxicillin, "B1", "2027-12-31", 10m, 11m, -1m, _satuanTablet);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtySelisih*");
    }

    [Theory]
    [InlineData(10, 8, 1)]
    [InlineData(10, 8, 3)]
    [InlineData(10, 0, 0)]
    public void ItemModel_WhenQtyBalanceMismatch_ShouldThrowArgumentException(
        decimal qtyKirim, decimal qtyTerimaBaik, decimal qtySelisih)
    {
        // AC-05: QtyTerimaBaik + QtySelisih == QtyKirim
        var act = () => new TerimaMutasiItemModel(
            1, _amoxicillin, "B1", "2027-12-31", qtyKirim, qtyTerimaBaik, qtySelisih, _satuanTablet, "selisih");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*QtyKirim*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConfirmed_WhenQtySelisihPositiveAndAlasanEmpty_ShouldThrowArgumentException(string emptyReason)
    {
        // AC-05
        var item = CreateItem(1, _amoxicillin, qtyKirim: 10m, qtyTerimaBaik: 7m, qtySelisih: 3m, alasanSelisih: emptyReason);

        var act = () => TerimaMutasiModel.CreateConfirmed(
            "MT2600001A", "", _gudangUtama, _depoRawatJalan, "USR01", new[] { item });

        act.Should().Throw<ArgumentException>()
            .WithMessage("*AlasanSelisih*");
    }

    [Fact]
    public void ItemModelCreateConfirmed_WhenQtySelisihPositiveAndAlasanEmpty_ShouldThrowArgumentException()
    {
        var act = () => TerimaMutasiItemModel.CreateConfirmed(
            1, _amoxicillin, "B1", "2027-12-31", 10m, 7m, 3m, _satuanTablet, "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*AlasanSelisih*");
    }

    #endregion

    #region CREATE REJECTED - POSITIVE & NEGATIVE

    [Fact]
    public void CreateRejected_WhenValid_ShouldSetStateRejectedAndZeroOutQtyTerimaBaik()
    {
        // AC-06
        // Arrange
        var sourceItems = new[]
        {
            CreateMutasiItem(1, _amoxicillin, qtyKirim: 15m),
            CreateMutasiItem(2, _paracetamol, qtyKirim: 25m)
        };

        // Act
        var sut = TerimaMutasiModel.CreateRejected(
            mutasiBrgId: "MT2600001A",
            orderMutasiId: "ORD001",
            layananAsal: _gudangUtama,
            layananTujuan: _depoRawatJalan,
            receiverUserId: "USR-REC-02",
            rejectionReason: "Kemasan segel rusak dan suhu tidak sesuai",
            listItem: sourceItems,
            note: "Ditolak saat serah terima",
            rejectDate: _baseDate);

        // Assert
        sut.TerimaMutasiId.Should().StartWith("TM");
        sut.TerimaMutasiId.Length.Should().BeLessThanOrEqualTo(10);
        sut.State.Should().Be(TerimaMutasiStateEnum.Rejected);
        sut.RejectionReason.Should().Be("Kemasan segel rusak dan suhu tidak sesuai");
        sut.ReceiverUserId.Should().Be("USR-REC-02");
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem.Should().AllSatisfy(item =>
        {
            item.QtyTerimaBaik.Should().Be(0m);
            item.QtySelisih.Should().Be(item.QtyKirim);
        });
        sut.ListItem[0].QtyKirim.Should().Be(15m);
        sut.ListItem[0].QtySelisih.Should().Be(15m);
        sut.ListItem[1].QtyKirim.Should().Be(25m);
        sut.ListItem[1].QtySelisih.Should().Be(25m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcd")]
    [InlineData("  ab  ")]
    public void CreateRejected_WhenRejectionReasonLessThan5Chars_ShouldThrowArgumentException(string shortReason)
    {
        // AC-06
        var sourceItems = new[] { CreateMutasiItem() };

        var act = () => TerimaMutasiModel.CreateRejected(
            "MT2600001A", "ORD001", _gudangUtama, _depoRawatJalan, "USR01", shortReason, sourceItems);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateRejected_WhenItemsEmpty_ShouldThrowArgumentException()
    {
        var act = () => TerimaMutasiModel.CreateRejected(
            "MT2600001A", "ORD001", _gudangUtama, _depoRawatJalan, "USR01", "Barang salah kirim", Array.Empty<MutasiBrgItemModel>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*kosong*");
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
            TerimaMutasiItemModel.Load(1, _amoxicillin, "BATCH-1", "2027-06-01", 10m, 8m, 2m, _satuanTablet, "Rusak")
        };

        // Act
        var sut = TerimaMutasiModel.Load(
            "TM2600001A",
            _baseDate,
            TerimaMutasiStateEnum.ReceivedWithDiscrepancy,
            "MT2600001A",
            "ORD001",
            _gudangUtama,
            _depoRawatJalan,
            "USR-REC-01",
            "Catatan",
            string.Empty,
            audit,
            items);

        // Assert
        sut.TerimaMutasiId.Should().Be("TM2600001A");
        sut.State.Should().Be(TerimaMutasiStateEnum.ReceivedWithDiscrepancy);
        sut.MutasiBrgId.Should().Be("MT2600001A");
        sut.OrderMutasiId.Should().Be("ORD001");
        sut.ListItem.Should().ContainSingle();
        sut.ListItem[0].QtyTerimaBaik.Should().Be(8m);
        sut.ListItem[0].QtySelisih.Should().Be(2m);
    }

    [Fact]
    public void TerimaMutasiModel_ShouldBeImmutableWithNoMutationMethods()
    {
        // AC-07: Tidak ada metode ubah, hapus, atau batal pada agregat
        var modelMethods = typeof(TerimaMutasiModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        modelMethods.Should().BeEmpty();

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
