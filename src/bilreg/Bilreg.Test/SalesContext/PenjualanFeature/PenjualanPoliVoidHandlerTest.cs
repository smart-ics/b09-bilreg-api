using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.SalesContext.PenjualanFeature;
using Bilreg.Application.SalesContext.PenjualanFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanPoliVoidHandlerTest
{
    private readonly Mock<IPenjualanRepo> _penjualanRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private readonly PenjualanPoliVoidHandler _sut;

    public PenjualanPoliVoidHandlerTest()
    {
        _sut = new PenjualanPoliVoidHandler(
            _penjualanRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _trsBillingRepoMock.Object,
            _jurnalRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _mediatorMock.Object);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 5, 11, 0, 0));
    }

    private static PenjualanModel CreatePenjualan(
        string penjualanId,
        string regId,
        string resepId = " ",
        bool isVoided = false)
    {
        var reg = new RegModel(
            regId,
            new DateOnly(2026, 11, 5),
            new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("MR01", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
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

        var layanan = LayananType.Default with { LayananId = "LYN-01", LayananName = "Poli Umum" };
        var tipeBrg = new TipeBrgReff("RJ", "Rawat Jalan");
        var auditTrail = isVoided
            ? new AuditTrailType(
                new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 9, 0, 0)),
                AuditInfoType.Default,
                new AuditInfoType("USER-01", new DateTime(2026, 11, 5, 9, 30, 0)))
            : AuditTrailType.Create("USER-01", new DateTime(2026, 11, 5, 9, 0, 0));

        var item = new PenjualanItemType(
            $"{penjualanId}-01",
            1,
            new BrgReff("BRG-01", "Paracetamol"),
            new SatuanType("TAB", "Tablet"),
            2m,
            EtiketType.Default,
            new NilaiItemType(10000m, 0, 0, 20000m, 0, 0, 0, 0, 0, 20000m),
            isVoided,
            []);

        return new PenjualanModel(
            penjualanId,
            resepId,
            reg.ToReff(),
            DokterType.Default.ToReff(),
            layanan.ToReff(),
            layanan.ToReff(),
            reg.TipeJaminan,
            tipeBrg,
            new NilaiPenjualanType(20000m, 0, 0, 0, 20000m, 0, 0, 0, 20000m),
            auditTrail,
            [item]);
    }

    [Fact]
    public async Task GivenValidDirectRegSaleAndOpenedBilling_WhenVoid_ThenSucceedsAndStockVoidedBillDeletedAndAuditSaved()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        const string transaksiId = $"{penjualanId}-01";
        const string regId = "REG-01";
        const string userId = "USER-01";
        const string reason = "Salah input item";

        var cmd = new PenjualanPoliVoidCmd(transaksiId, userId, reason, "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(transaksiId, regId, 20000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var voidResult = new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.Success,
            [],
            string.Empty, string.Empty, string.Empty, string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidResult);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        penjualan.AuditTrail.IsVoided.Should().BeTrue();
        tataRekening.ListTrsBill.Should().NotContain(x => x.TrsBillingId == transaksiId);

        var expectedVoidTrsReffId = penjualanId.Length <= 8
            ? $"{penjualanId}_V"
            : $"{penjualanId[..8]}_V";

        _mediatorMock.Verify(x => x.Send(
            It.Is<PostSaleVoidConsequenceCommand>(c =>
                c.OriginalSaleTrsReffId == penjualanId &&
                c.VoidTrsReffId == expectedVoidTrsReffId &&
                c.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _penjualanRepoMock.Verify(x => x.SaveChanges(penjualan), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == transaksiId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == transaksiId)), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "VOID" &&
            a.EntityName == nameof(PenjualanModel) &&
            a.EntityId == penjualanId &&
            a.Reason == reason)), Times.Once);
    }

    [Fact]
    public async Task GivenPenjualanWithResepId_WhenVoid_ThenThrowsInvalidOperationException()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        const string cmdTransaksiId = $"{penjualanId}-01";
        const string regId = "REG-01";

        var cmd = new PenjualanPoliVoidCmd(cmdTransaksiId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, regId, resepId: "RS20261105001");

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat dibatalkan dari modul poliklinik*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GivenPenjualanAlreadyVoided_WhenVoid_ThenThrowsInvalidOperationException()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        var cmd = new PenjualanPoliVoidCmd(penjualanId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, "REG-01", isVoided: true);

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah void*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenBillingLockedClosedOrLunas_WhenVoid_ThenThrowsInvalidOperationException()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        const string transaksiId = $"{penjualanId}-01";
        const string regId = "REG-01";

        var cmd = new PenjualanPoliVoidCmd(transaksiId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(transaksiId, regId, 20000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Closed, bill);

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GivenStockVoidFails_WhenVoid_ThenThrowsInvalidOperationException()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        const string transaksiId = $"{penjualanId}-01";
        const string regId = "REG-01";

        var cmd = new PenjualanPoliVoidCmd(transaksiId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(transaksiId, regId, 20000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var voidResult = new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.AbortedInconsistent,
            [],
            string.Empty, "Inconsistent stock ledger state", "BRG-01", string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidResult);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pengembalian stok gagal*AbortedInconsistent*");

        _penjualanRepoMock.Verify(x => x.SaveChanges(It.IsAny<PenjualanModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GivenPenjualanNotFound_WhenVoid_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new PenjualanPoliVoidCmd("DUUNKNOWN", "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        _penjualanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPenjualanKey>()))
            .Returns(MayBe<PenjualanModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*DUUNKNOWN*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenEmptyVoidReason_WhenVoid_ThenThrowsArgumentException(string voidReason)
    {
        // Arrange
        var cmd = new PenjualanPoliVoidCmd("DU261105001A", "USER-01", voidReason, "127.0.0.1", "TestAgent");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenTransaksiIdWithoutSuffix_WhenVoid_ThenExtractsPenjualanIdCorrectly()
    {
        // Arrange
        const string penjualanId = "DU261105001A";
        const string regId = "REG-01";

        var cmd = new PenjualanPoliVoidCmd(penjualanId, "USER-01", "Alasan valid", "127.0.0.1", "TestAgent");
        var penjualan = CreatePenjualan(penjualanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill($"{penjualanId}-01", regId, 20000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _penjualanRepoMock.Setup(x => x.LoadEntity(It.Is<IPenjualanKey>(k => k.PenjualanId == penjualanId)))
            .Returns(MayBe.From(penjualan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var voidResult = new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.Success,
            [],
            string.Empty, string.Empty, string.Empty, string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidResult);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        penjualan.AuditTrail.IsVoided.Should().BeTrue();
        _penjualanRepoMock.Verify(x => x.SaveChanges(penjualan), Times.Once);
    }
}
