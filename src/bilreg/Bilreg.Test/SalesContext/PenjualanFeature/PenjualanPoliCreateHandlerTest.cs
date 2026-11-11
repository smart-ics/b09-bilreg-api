using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.SalesContext.PenjualanFeature;
using Bilreg.Application.SalesContext.PenjualanFeature.UseCases;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanPoliCreateHandlerTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepoMock = new();
    private readonly Mock<IBrgPricingService> _pricingServiceMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IStockBatchRepo> _batchRepoMock = new();
    private readonly Mock<IPenjualanRepo> _penjualanRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillServiceMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();

    private readonly PenjualanPoliCreateHandler _sut;

    public PenjualanPoliCreateHandlerTest()
    {
        _sut = new PenjualanPoliCreateHandler(
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _jaminanRepoMock.Object,
            _brgRepoMock.Object,
            _groupRekRepoMock.Object,
            _mapJaminanJkRepoMock.Object,
            _pricingServiceMock.Object,
            _tglJamProviderMock.Object,
            _mediatorMock.Object,
            _batchRepoMock.Object,
            _penjualanRepoMock.Object,
            _addBillServiceMock.Object,
            _jurnalRepoMock.Object);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 5, 10, 0, 0));
        _mapJaminanJkRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe<MapJaminanJkType>.None);
    }

    private void SetupBasicMasterData(
        string regId = "REG-01",
        string lynId = "LYN-01",
        string brgId = "BRG-01",
        string jaminanId = "J01",
        string groupRekId = "GR01",
        decimal unitPrice = 10000m,
        decimal qty = 2m)
    {
        var reg = new RegModel(
            regId,
            new DateOnly(2026, 11, 5),
            new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff($"{jaminanId}01", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var layanan = LayananType.Default with { LayananId = lynId, LayananName = "Poli Umum" };
        var jaminan = JaminanType.Default with { JaminanId = jaminanId, JaminanName = "BPJS Kesehatan" };

        var brgMock = new Mock<IBrg>();
        brgMock.Setup(x => x.BrgId).Returns(brgId);
        brgMock.Setup(x => x.BrgName).Returns("Paracetamol");
        brgMock.Setup(x => x.GroupRek).Returns(new GroupRekReff(groupRekId, "Obat"));
        brgMock.Setup(x => x.SatuanKecil).Returns(new SatuanType("TAB", "Tablet"));
        brgMock.Setup(x => x.ToReff()).Returns(new BrgReff(brgId, "Paracetamol"));

        var groupRek = new GroupRekType(
            groupRekId, "Obat", GroupRekDkType.Default, "RC01",
            CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default);

        var total = unitPrice * qty;
        var pricing = new BrgPriceCalcResult(
            new TipeBrgReff("RJ", "Rawat Jalan"),
            new NilaiItemType(unitPrice, 0, 0, total, 0, 0, 0, 0, 0, total));

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == lynId)))
            .Returns(MayBe.From(layanan));
        _jaminanRepoMock.Setup(x => x.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == jaminanId)))
            .Returns(MayBe.From(jaminan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == brgId)))
            .Returns(MayBe.From(brgMock.Object));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.Is<IGroupRekKey>(k => k.GroupRekId == groupRekId)))
            .Returns(MayBe.From(groupRek));
        _pricingServiceMock.Setup(x => x.CalculateItemPrice(brgId, qty, $"{jaminanId}01", lynId))
            .Returns(pricing);

        var issueLines = new List<PostSaleIssueLineResult>
        {
            new("STM01", "STL01", "STB01", "DO-01", "BK01", "STK01", qty)
        };
        var issueResult = new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.Success,
            issueLines,
            0, string.Empty, string.Empty, string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(issueResult);

        var location = LocationStockBalanceModel.Create("STB01", brgId, "DO-01", lynId, new DateTime(2027, 1, 1), new DateTime(2026, 1, 1), 100, "BATCH-ABC");
        var stockBatch = new StockBatchModel(
            "STB01", brgId, "DO-01", 100, 5000m, new DateTime(2026, 1, 1), "PO-01", 1, [location]);
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, "DO-01"))
            .Returns(MayBe.From(stockBatch));

        _addBillServiceMock.Setup(x => x.FromPenjualanItem(
                It.IsAny<PenjualanModel>(),
                It.IsAny<PenjualanItemType>(),
                It.IsAny<RegModel>(),
                It.IsAny<JaminanType>(),
                It.IsAny<GroupRekType>(),
                It.IsAny<DateTime>()))
            .Returns((PenjualanModel p, PenjualanItemType i, RegModel r, JaminanType j, GroupRekType gr, DateTime d) =>
                TrsBillFactory.CreateFromPenjualanItem(p, i, r, j, gr, d));
    }

    [Fact]
    public async Task GivenValidRequest_WhenHandle_ThenCreatesDirectRegSaleAndPersistsBillAndJurnal()
    {
        // Arrange
        const string regId = "REG-01";
        const string lynId = "LYN-01";
        const string brgId = "BRG-01";
        const decimal qty = 2m;
        const string userId = "USER-01";

        SetupBasicMasterData(regId, lynId, brgId, qty: qty);
        var cmd = new PenjualanPoliCreateCmd(regId, lynId, brgId, qty, userId);

        PenjualanModel? savedPenjualan = null;
        _penjualanRepoMock.Setup(x => x.SaveChanges(It.IsAny<PenjualanModel>()))
            .Callback<PenjualanModel>(p => savedPenjualan = p);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PenjualanId.Should().StartWith("DU");
        response.PenjualanItemId.Should().Be($"{response.PenjualanId}-01");
        response.Total.Should().Be(20000m);

        savedPenjualan.Should().NotBeNull();
        savedPenjualan!.PenjualanId.Should().Be(response.PenjualanId);
        savedPenjualan.ResepId.Should().Be(" ");
        savedPenjualan.ListItem.Should().HaveCount(1);

        var savedItem = savedPenjualan.ListItem.First();
        savedItem.PenjualanItemId.Should().Be(response.PenjualanItemId);
        savedItem.Qty.Should().Be(qty);
        savedItem.ListBatch.Should().HaveCount(1);
        savedItem.ListBatch[0].NoBatch.Should().Be("BATCH-ABC");
        savedItem.ListBatch[0].PoId.Should().Be("PO-01");
        savedItem.ListBatch[0].DoId.Should().Be("DO-01");
        savedItem.ListBatch[0].Hpp.Should().Be(5000m);
        savedItem.ListBatch[0].Qty.Should().Be(qty);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PostSaleIssueConsequenceCommand>(c =>
                c.BrgId == brgId &&
                c.LayananId == lynId &&
                c.Qty == qty &&
                c.SaleKind == SaleIssueKindEnum.Du &&
                c.TrsReffId == response.PenjualanId),
            It.IsAny<CancellationToken>()), Times.Once);

        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Once);
        _addBillServiceMock.Verify(x => x.FromPenjualanItem(
            It.IsAny<PenjualanModel>(),
            It.IsAny<PenjualanItemType>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Once);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Once);
    }

    [Fact]
    public async Task GivenInsufficientStock_WhenHandle_ThenThrowsInvalidOperationExceptionAndDoesNotSavePenjualan()
    {
        // Arrange
        SetupBasicMasterData();
        var cmd = new PenjualanPoliCreateCmd("REG-01", "LYN-01", "BRG-01", 2m, "USER-01");

        var issueResult = new PostSaleIssueConsequenceResult(
            PostSaleIssueOutcomeEnum.InsufficientStock,
            [],
            2m, "Stock not available", "BRG-01", string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(issueResult);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pengeluaran stok gagal*InsufficientStock*");

        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Never);
        _addBillServiceMock.Verify(x => x.FromPenjualanItem(
            It.IsAny<PenjualanModel>(),
            It.IsAny<PenjualanItemType>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<GroupRekType>(),
            It.IsAny<DateTime>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Never);
    }

    [Fact]
    public async Task GivenBillingLocked_WhenAddBillThrows_ThenExceptionBubblesAndRollsBack()
    {
        // Arrange
        SetupBasicMasterData();
        var cmd = new PenjualanPoliCreateCmd("REG-01", "LYN-01", "BRG-01", 2m, "USER-01");

        _addBillServiceMock.Setup(x => x.FromPenjualanItem(
                It.IsAny<PenjualanModel>(),
                It.IsAny<PenjualanItemType>(),
                It.IsAny<RegModel>(),
                It.IsAny<JaminanType>(),
                It.IsAny<GroupRekType>(),
                It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Operasi bill hanya diizinkan saat TataRekening untuk registrasi 'REG-01' berstatus OPEN."));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");

        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Never);
    }

    [Fact]
    public async Task GivenInactiveRegistration_WhenHandle_ThenThrowsArgumentException()
    {
        // Arrange
        SetupBasicMasterData();
        var regInactive = new RegModel(
            "REG-01",
            new DateOnly(2026, 11, 5),
            new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 9, 0, 0)), // RegVoidAudit
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("J0101", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(regInactive));

        var cmd = new PenjualanPoliCreateCmd("REG-01", "LYN-01", "BRG-01", 2m, "USER-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak aktif*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GivenInvalidQty_WhenHandle_ThenThrowsArgumentException(decimal qty)
    {
        // Arrange
        var cmd = new PenjualanPoliCreateCmd("REG-01", "LYN-01", "BRG-01", qty, "USER-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Qty harus lebih besar dari 0*");
    }

    [Fact]
    public async Task GivenNonExistentRegistration_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new PenjualanPoliCreateCmd("REG-UNKNOWN", "LYN-01", "BRG-01", 2m, "USER-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*REG-UNKNOWN*");
    }
}
