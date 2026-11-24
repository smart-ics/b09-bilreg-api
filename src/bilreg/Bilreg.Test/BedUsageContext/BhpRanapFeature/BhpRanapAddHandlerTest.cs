using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BhpRanapFeature;
using Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BhpRanapFeature;

public class BhpRanapAddHandlerTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock;
    private readonly Mock<IBhpRanapRepo> _bhpRanapRepoMock;
    private readonly Mock<IRegRepo> _regRepoMock;
    private readonly Mock<ILayananRepo> _layananRepoMock;
    private readonly Mock<IJaminanRepo> _jaminanRepoMock;
    private readonly Mock<IBrgRepo> _brgRepoMock;
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock;
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepoMock;
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock;
    private readonly Mock<IAddBillAppService> _addBillServiceMock;
    private readonly Mock<IJurnalRepo> _jurnalRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly BhpRanapAddHandler _sut;

    public BhpRanapAddHandlerTest()
    {
        _pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        _bhpRanapRepoMock = new Mock<IBhpRanapRepo>();
        _regRepoMock = new Mock<IRegRepo>();
        _layananRepoMock = new Mock<ILayananRepo>();
        _jaminanRepoMock = new Mock<IJaminanRepo>();
        _brgRepoMock = new Mock<IBrgRepo>();
        _groupRekRepoMock = new Mock<IGroupRekRepo>();
        _mapJaminanJkRepoMock = new Mock<IMapJaminanJkRepo>();
        _tataRekeningRepoMock = new Mock<ITataRekeningRepo>();
        _addBillServiceMock = new Mock<IAddBillAppService>();
        _jurnalRepoMock = new Mock<IJurnalRepo>();
        _mediatorMock = new Mock<IMediator>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 2, 11, 10, 0, 0));
        _mapJaminanJkRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe<MapJaminanJkType>.None);
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        _sut = new BhpRanapAddHandler(
            _pakaiBedRepoMock.Object,
            _bhpRanapRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _jaminanRepoMock.Object,
            _brgRepoMock.Object,
            _groupRekRepoMock.Object,
            _mapJaminanJkRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _addBillServiceMock.Object,
            _jurnalRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private void SetupBasicMasterData(
        string regId = "REG001",
        string layananId = "3RI02",
        string brgId = "BHP10",
        string jaminanId = "001",
        string groupRekId = "GR01")
    {
        var reg = new RegModel(
            regId,
            new DateOnly(2026, 2, 11),
            new AuditInfoType("USER1", new DateTime(2026, 2, 11, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff($"{jaminanId}01", "Umum"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        var layanan = LayananType.Default with { LayananId = layananId, LayananName = "Bangsal Melati" };
        var jaminan = JaminanType.Default with { JaminanId = jaminanId, JaminanName = "Umum" };

        var brgMock = new Mock<IBrg>();
        brgMock.Setup(x => x.BrgId).Returns(brgId);
        brgMock.Setup(x => x.BrgName).Returns("Spuit 10cc");
        brgMock.Setup(x => x.IsAktif).Returns(true);
        brgMock.Setup(x => x.GroupRek).Returns(new GroupRekReff(groupRekId, "Alkes/BHP"));
        brgMock.Setup(x => x.SatuanKecil).Returns(new SatuanType("PCS", "Pieces"));
        brgMock.Setup(x => x.ToReff()).Returns(new BrgReff(brgId, "Spuit 10cc"));

        var groupRek = new GroupRekType(
            groupRekId, "Alkes/BHP", GroupRekDkType.Default, "RC01",
            CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == layananId)))
            .Returns(MayBe.From(layanan));
        _jaminanRepoMock.Setup(x => x.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == jaminanId)))
            .Returns(MayBe.From(jaminan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == brgId)))
            .Returns(MayBe.From(brgMock.Object));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.Is<IGroupRekKey>(k => k.GroupRekId == groupRekId)))
            .Returns(MayBe.From(groupRek));

        var issueResult = new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.Success,
            [],
            0, string.Empty, string.Empty, string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(issueResult);

        _addBillServiceMock.Setup(x => x.FromBhpRanap(
                It.IsAny<BhpRanapModel>(),
                It.IsAny<RegModel>(),
                It.IsAny<LayananType>(),
                It.IsAny<JaminanType>(),
                It.IsAny<GroupRekType>(),
                It.IsAny<DateTime>()))
            .Returns((BhpRanapModel b, RegModel r, LayananType l, JaminanType j, GroupRekType gr, DateTime d) =>
                TrsBillFactory.CreateFromBhpRanap(b, r, l, j, gr, d));
    }

    [Fact]
    public async Task Handle_WithPakaiBedInRepo_ShouldSaveAndReturnResponse()
    {
        // Arrange
        SetupBasicMasterData();

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 3,
            Price: 5000m,
            UserId: "USER1");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.BhpRanapId.Should().StartWith("DU");
        response.BhpRanapId.Length.Should().Be(10);
        response.PakaiBedId.Should().Be("BED001");
        response.RegId.Should().Be("REG001");
        response.Subtotal.Should().Be(15000m);

        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.Is<BhpRanapModel>(b =>
            b.BhpItemId == "BHP10" &&
            b.Qty == 3 &&
            b.Price == 5000m &&
            b.RegId == "REG001" &&
            b.LayananId == "3RI02")), Times.Once);

        _mediatorMock.Verify(x => x.Send(It.Is<PostSaleIssueConsequenceCommand>(c =>
            c.BrgId == "BHP10" &&
            c.LayananId == "3RI02" &&
            c.Qty == 3 &&
            c.TrsReffId == response.BhpRanapId &&
            c.SaleKind == SaleIssueKindEnum.Du), It.IsAny<CancellationToken>()), Times.Once);

        _addBillServiceMock.Verify(x => x.FromBhpRanap(
            It.Is<BhpRanapModel>(b => b.BhpRanapId == response.BhpRanapId),
            It.Is<RegModel>(r => r.RegId == "REG001"),
            It.Is<LayananType>(l => l.LayananId == "3RI02"),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Once);

        _jurnalRepoMock.Verify(x => x.SaveChanges(It.Is<JurnalType>(j =>
            j.JurnalId == response.BhpRanapId &&
            j.Reg.RegId == "REG001")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExplicitRegIdAndLayananId_SeparatesBabyAndMother()
    {
        // Arrange — Bed occupied by mother (REG-IBU), but request targets baby (REG-BAYI)
        SetupBasicMasterData(regId: "REG-BAYI", layananId: "3RI-PERI");

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG-IBU", "MR-IBU", "Ibu Melati"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 2,
            Price: 5000m,
            UserId: "USER1",
            RegId: "REG-BAYI",
            LayananId: "3RI-PERI");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.RegId.Should().Be("REG-BAYI");
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.Is<BhpRanapModel>(b =>
            b.RegId == "REG-BAYI" && b.LayananId == "3RI-PERI")), Times.Once);

        _mediatorMock.Verify(x => x.Send(It.Is<PostSaleIssueConsequenceCommand>(c =>
            c.LayananId == "3RI-PERI" && c.Qty == 2), It.IsAny<CancellationToken>()), Times.Once);

        _addBillServiceMock.Verify(x => x.FromBhpRanap(
            It.IsAny<BhpRanapModel>(),
            It.Is<RegModel>(r => r.RegId == "REG-BAYI"),
            It.Is<LayananType>(l => l.LayananId == "3RI-PERI"),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ThrowsInvalidOperationExceptionAndDoesNotPersistBilling()
    {
        // Arrange
        SetupBasicMasterData();

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var issueResult = new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.InsufficientStock,
            [],
            5, "Saldo stok tidak cukup", "BHP10", string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(issueResult);

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 10,
            Price: 5000m,
            UserId: "USER1");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pengeluaran stok gagal*");

        _addBillServiceMock.Verify(x => x.FromBhpRanap(
            It.IsAny<BhpRanapModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<LayananType>(),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Never);

        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupBasicMasterData();

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var closedTataRekening = TataRekeningModel.Create("REG001");
        closedTataRekening.Close();

        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(closedTataRekening));

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 2,
            Price: 5000m,
            UserId: "USER1");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Operasi bill hanya diizinkan saat TataRekening*berstatus OPEN*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegisterNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG_UNKNOWN", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 1,
            Price: 5000m,
            UserId: "USER1");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Register 'REG_UNKNOWN' not found*");
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupBasicMasterData();
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP_NONEXISTENT",
            BhpItemName: "Non Existent",
            Qty: 1,
            Price: 5000m,
            UserId: "USER1");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Barang 'BHP_NONEXISTENT' not found*");
    }

    [Fact]
    public async Task Handle_WhenStockIssueOutcomeIsIdempotent_SucceedsAndPersistsBilling()
    {
        // Arrange
        SetupBasicMasterData();

        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var idempotentResult = new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.Idempotent,
            [],
            0, string.Empty, string.Empty, string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(idempotentResult);

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 2,
            Price: 5000m,
            UserId: "USER1");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Subtotal.Should().Be(10000m);
        _addBillServiceMock.Verify(x => x.FromBhpRanap(
            It.IsAny<BhpRanapModel>(),
            It.Is<RegModel>(r => r.RegId == "REG001"),
            It.Is<LayananType>(l => l.LayananId == "3RI02"),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Once);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPakaiBedNotFoundAndNoExplicitIds_ThrowsInvalidOperationException()
    {
        // Arrange
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED_UNKNOWN",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 1,
            Price: 5000m,
            UserId: "USER1");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RegId tidak ditemukan*");
    }
}
