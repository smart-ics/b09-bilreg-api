using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryCreateDraftCommandTest
{
    private const string SuratJalan = "SJ-TEST-12345";
    private const string PoId = "PO26100001";
    private const string SupplierId = "SUP001";
    private const string SupplierName = "PT Kimia Farma";
    private const string GudangId = "GDFAR";
    private const string GudangName = "Gudang Farmasi Utama";
    private const string UserId = "USR01";
    private const string Keterangan = "Draf penerimaan barang logistik";

    private readonly Mock<IDeliveryOrderRepo> _repoMock;
    private readonly Mock<IDeliveryValidationDal> _validationDalMock;
    private readonly Mock<IPoDeliveryTrackingDal> _poTrackingDalMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly DateTime _now;
    private readonly DeliveryCreateDraftHandler _sut;

    public DeliveryCreateDraftCommandTest()
    {
        _repoMock = new Mock<IDeliveryOrderRepo>();
        _validationDalMock = new Mock<IDeliveryValidationDal>();
        _poTrackingDalMock = new Mock<IPoDeliveryTrackingDal>();
        _clockMock = new Mock<ITglJamProvider>();

        _now = new DateTime(2026, 10, 15, 10, 0, 0);
        _clockMock.Setup(c => c.Now).Returns(_now);

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(true);

        _sut = new DeliveryCreateDraftHandler(
            _repoMock.Object,
            _validationDalMock.Object,
            _poTrackingDalMock.Object,
            _clockMock.Object);
    }

    private static List<PoQuotaDto> CreateSamplePoQuotas(string layananId = GudangId)
    {
        return
        [
            new PoQuotaDto(
                poId: PoId,
                noUrut: 1,
                brgId: "BRG001",
                brgName: "Paracetamol 500mg",
                satuanId: "BOX",
                qtyPo: 10m,
                qtyDo: 0m,
                sisaQty: 10m,
                isClosed: false,
                layananId: layananId,
                konversi: 10,
                harga: 50000m,
                diskon: 0m),
            new PoQuotaDto(
                poId: PoId,
                noUrut: 2,
                brgId: "BRG002",
                brgName: "Amoxicillin 500mg",
                satuanId: "BOX",
                qtyPo: 20m,
                qtyDo: 5m,
                sisaQty: 15m,
                isClosed: false,
                layananId: layananId,
                konversi: 10,
                harga: 75000m,
                diskon: 0m)
        ];
    }

    private List<DeliveryCreateDraftItemCommand> CreateSampleItems(DateTime? edDate = null)
    {
        var ed = edDate ?? _now.AddMonths(24);
        return
        [
            new DeliveryCreateDraftItemCommand(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-001",
                TglEd: ed,
                QtyKirim: 10m,
                QtyDiterima: 10m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        ];
    }

    [Fact]
    public async Task Handle_WhenValidDraftWithPo_CreatesDraftAndSavesModel()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            SupplierName: SupplierName,
            GudangName: GudangName,
            BiayaLain: 1000m,
            DiskonLain: 500m,
            DeliveryOrderDate: _now);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().StartWith("DM");
        response.DoId.Should().Be(response.DeliveryOrderId);

        _validationDalMock.Verify(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, null), Times.Once);
        _poTrackingDalMock.Verify(p => p.GetRemainingQuota(PoId), Times.Once);
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);

        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Draft);
        savedModel.SuratJalanNo.Should().Be(SuratJalan);
        savedModel.PoId.Should().Be(PoId);
        savedModel.Supplier.PartnerId.Should().Be(SupplierId);
        savedModel.Gudang.LayananId.Should().Be(GudangId);
        savedModel.ListItem.Should().HaveCount(1);
        savedModel.BiayaLain.Should().Be(1000m);
        savedModel.DiskonLain.Should().Be(500m);
        savedModel.AuditTrail.Created.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WhenValidDraftWithoutPo_CreatesDraftAndSavesModel()
    {
        // ARRANGE
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var items = new List<DeliveryCreateDraftItemCommand>
        {
            new(
                BrgId: "BRG-BEBAS",
                SatuanId: "BOX",
                Konversi: 1,
                Harga: 25000m,
                Diskon: 0m,
                TaxPercentage: 0m,
                NoBatch: "LOT-999",
                TglEd: _now.AddMonths(20),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Kassa Steril",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 0)
        };

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: string.Empty,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: "Pembelian bebas tanpa PO",
            UserId: UserId,
            Items: items,
            SupplierName: SupplierName,
            GudangName: GudangName,
            DeliveryOrderDate: _now);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().StartWith("DM");
        _poTrackingDalMock.Verify(p => p.GetRemainingQuota(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Draft);
        savedModel.PoId.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSuratJalanNotUnique_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, null))
            .Returns(false);

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{SuratJalan}*{SupplierId}*sudah pernah diterima*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPoNotFoundOrNoActiveLines_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(new List<PoQuotaDto>());

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{PoId}*tidak ditemukan atau tidak memiliki item aktif*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWarehouseDoesNotMatchPo_ThrowsInvalidOperationException()
    {
        // ARRANGE: PO target warehouse is GDFAR, command has GDLOG
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas(layananId: "GDFAR"));

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: "GDLOG",
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*GDLOG*tidak sesuai dengan gudang tujuan PO*GDFAR*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsPoRemainingQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE: Quota is 10, receiving 15
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var items = new List<DeliveryCreateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-OVER",
                TglEd: _now.AddMonths(24),
                QtyKirim: 15m,
                QtyDiterima: 15m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items,
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Kuantitas diterima (15)*melebihi sisa kuota pesanan (10)*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBatchSplitAggregateExceedsQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE: Quota is 10, two split lines of 6 and 5 (total 11 > 10)
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var items = new List<DeliveryCreateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-A",
                TglEd: _now.AddMonths(24),
                QtyKirim: 6m,
                QtyDiterima: 6m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1),
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-B",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items,
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Kuantitas diterima (11)*melebihi sisa kuota pesanan (10)*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBonusItemExceedsPoQuota_AllowedWithoutError()
    {
        // ARRANGE: Quota is 10, regular item 10, plus bonus item 5 of same item
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var items = new List<DeliveryCreateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-REGULAR",
                TglEd: _now.AddMonths(24),
                QtyKirim: 10m,
                QtyDiterima: 10m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1),
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 0m,
                Diskon: 0m,
                TaxPercentage: 0m,
                NoBatch: "BATCH-BONUS",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: true,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items,
            DeliveryOrderDate: _now);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().StartWith("DM");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemsEmpty_ThrowsArgumentException()
    {
        // ARRANGE
        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: new List<DeliveryCreateDraftItemCommand>(),
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Daftar item draf penerimaan barang tidak boleh kosong*");
    }

    [Theory]
    [InlineData("", "SUP001", "GDFAR", "USR01")]
    [InlineData("SJ-01", "", "GDFAR", "USR01")]
    [InlineData("SJ-01", "SUP001", "", "USR01")]
    [InlineData("SJ-01", "SUP001", "GDFAR", "")]
    public async Task Handle_WhenRequiredHeaderFieldEmpty_ThrowsArgumentException(
        string sj, string supp, string gudang, string user)
    {
        // ARRANGE
        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: sj,
            PoId: PoId,
            SupplierId: supp,
            GudangId: gudang,
            Keterangan: Keterangan,
            UserId: user,
            Items: CreateSampleItems(),
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenShelfLifeUnder18MonthsWithoutGuaranteeLetter_ThrowsArgumentException()
    {
        // ARRANGE: ED is 12 months from now, without guarantee letter
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var items = new List<DeliveryCreateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-SHORT-ED",
                TglEd: _now.AddMonths(12),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items,
            DeliveryOrderDate: _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*kurang dari 18 bulan*wajib menyertakan surat jaminan retur*");
    }

    [Fact]
    public async Task Handle_WhenDeliveryOrderDateHasNoTime_CombinesWithCurrentTime()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var dateOnly = new DateTime(2026, 10, 21); // 00:00:00
        var command = new DeliveryCreateDraftCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: dateOnly);

        // ACT
        await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        savedModel.Should().NotBeNull();
        savedModel!.DeliveryOrderDate.Should().Be(new DateTime(2026, 10, 21, 10, 0, 0));
        savedModel.DeliveryOrderDate.TimeOfDay.Should().Be(_now.TimeOfDay);
    }
}
