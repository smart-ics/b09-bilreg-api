using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;
using Xunit.Abstractions;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanVoidCmdTest
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<IBillingLockValidationService> _billingLockValidatorMock = new();

    private readonly TindakanVoidHandler _sut;
    private readonly DateTime _testNow = new(2026, 10, 20, 11, 0, 0);

    public TindakanVoidCmdTest(ITestOutputHelper output)
    {
        _output = output;
        _tglJamProviderMock.Setup(x => x.Now).Returns(_testNow);

        _sut = new TindakanVoidHandler(
            _tdkRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _trsBillingRepoMock.Object,
            _jurnalRepoMock.Object,
            _billingLockValidatorMock.Object);
    }

    private static TindakanModel CreateTestTindakan(string tindakanId = "TDK-001", string regId = "REG01")
    {
        var regReff = new RegReff(regId, "PAS01", "Pasien A");
        var layananReff = new LayananReff("LAY01", "Layanan A");
        var kelasReff = new KelasReff("K1", "Kelas 1");
        var tipeTarifReff = new TipeTarifReff("TT", "Umum");
        var tarifReff = new TarifReff("TRF01", "Tarif Tindakan");
        var komponen = new TindakanKomponenWithoutPpaType(
            new KomponenReff("K1", "Jasa Sarana"), 1, 50000, 2, 100000);
        var auditTrail = AuditTrailType.Create("USR01", new DateTime(2026, 10, 20));

        return new TindakanModel(
            tindakanId, new DateTime(2026, 10, 20), "ORD001",
            regReff, layananReff, kelasReff, tipeTarifReff, tarifReff,
            [komponen], auditTrail, "BED-01", 2);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningIsOpened_ShouldVoidSuccessfullyAndPersistChanges()
    {
        // Arrange
        const string tdkId = "TDK-001";
        const string regId = "REG01";
        var tdk = CreateTestTindakan(tdkId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tdkId, regId, 100000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == tdkId)))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new TindakanVoidCmd(tdkId, "SUPERVISOR", "Salah input pasien", "127.0.0.1", "Mozilla/5.0");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        tdk.AuditTrail.IsVoided.Should().BeTrue();
        tdk.AuditTrail.Voided.UserId.Should().Be("SUPERVISOR");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.Is<TindakanModel>(t => t.AuditTrail.IsVoided)), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.Is<TataRekeningModel>(tr => !tr.ListTrsBill.Any(b => b.TrsBillingId == tdkId))), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == tdkId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == tdkId)), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "VOID" &&
            a.EntityId == tdkId &&
            a.Reason == "Salah input pasien")), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningIsClosed_ShouldFailClosedAndThrowInvalidOperationException()
    {
        // Arrange
        const string tdkId = "TDK-001";
        const string regId = "REG01";
        var tdk = CreateTestTindakan(tdkId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tdkId, regId, 100000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(regId, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == tdkId)))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new TindakanVoidCmd(tdkId, "SUPERVISOR", "Salah input", "127.0.0.1", "Mozilla/5.0");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Operasi bill hanya diizinkan saat TataRekening untuk registrasi 'REG01' berstatus OPEN*");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningIsFinalized_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        const string tdkId = "TDK-001";
        const string regId = "REG01";
        var tdk = CreateTestTindakan(tdkId, regId);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == tdkId)))
            .Returns(MayBe.From(tdk));

        _billingLockValidatorMock
            .Setup(x => x.EnsureCanModifyBillingAsync(regId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BillingLockViolationException(regId, TataRekeningStatusEnum.Finalized));

        var cmd = new TindakanVoidCmd(tdkId, "SUPERVISOR", "Salah input", "127.0.0.1", "Mozilla/5.0");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BillingLockViolationException>()
            .WithMessage($"*Episode billing '{regId}' telah berstatus Finalized*");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningIsLunas_ShouldFailClosedAndThrowInvalidOperationException()
    {
        // Arrange
        const string tdkId = "TDK-001";
        const string regId = "REG01";
        var tdk = CreateTestTindakan(tdkId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tdkId, regId, 100000m);
        var tataRekening = new TataRekeningModel(
            regId, TataRekeningStatusEnum.Lunas, TataRekeningFinalizationType.Default, [], [bill]);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == tdkId)))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new TindakanVoidCmd(tdkId, "SUPERVISOR", "Salah input", "127.0.0.1", "Mozilla/5.0");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TataRekening sudah LUNAS dan tidak dapat dimodifikasi*");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTindakanNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _tdkRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITindakanKey>()))
            .Returns(MayBe<TindakanModel>.None);

        var cmd = new TindakanVoidCmd("TDK-MISSING", "USER01", "Alasan", "127.0.0.1", "Mozilla/5.0");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tindakan TDK-MISSING not found*");
    }

    [Fact]
    public async Task Handle_WhenBillingIdTruncatedByLegacyTable_ShouldMatchPrefixAndVoidSuccessfully()
    {
        // Backward-compatibility: Legacy TA_TRS_TINDAKAN truncated ID to 10 chars, while ta_trs_billing has full 12 chars.
        // For modern BILRG_Tindakan, IDs are unconstrained up to 26 chars, but prefix matching ensures historical records can still be voided.
        const string legacyTdkId = "TDK09KZ0XH";
        const string fullBillingId = "TDK09KZ0XHX7";
        const string regId = "RG01377882";

        var tdk = CreateTestTindakan(legacyTdkId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(fullBillingId, regId, 700000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == legacyTdkId)))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new TindakanVoidCmd(legacyTdkId, "SPR001", "Salah input tindakan bangsal", "127.0.0.1", "Mozilla/5.0");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        tdk.AuditTrail.IsVoided.Should().BeTrue();
        tdk.AuditTrail.Voided.UserId.Should().Be("SPR001");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.Is<TindakanModel>(t => t.AuditTrail.IsVoided)), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.Is<TataRekeningModel>(tr => !tr.ListTrsBill.Any(b => b.TrsBillingId == fullBillingId))), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == fullBillingId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == fullBillingId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTindakanHasBomItems_ShouldCascadeVoidPakaiBrgAndTrsBillingAndMarkVoid()
    {
        // Arrange
        const string tdkId = "TDK-001";
        const string regId = "REG01";
        var tdk = CreateTestTindakan(tdkId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tdkId, regId, 100000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        var tdkBarangRepoMock = new Mock<ITindakanBarangRepo>();
        var tdkStokPendingRepoMock = new Mock<ITindakanStokPendingRepo>();
        var pakaiBrgInternalServiceMock = new Mock<IPakaiBrgInternalService>();

        var bomItem = new TindakanBarangType(
            TindakanBarangId: "TB-01",
            TindakanId: tdkId,
            NoUrut: 1,
            BrgId: "BRG-01",
            SatuanBomId: "PCS",
            QtyBom: 2m,
            SatuanKecilId: "PCS",
            QtyKecilRequested: 2m,
            QtyKecilDeducted: 2m,
            QtyKecilPending: 0m,
            IsChargeBarang: true,
            HargaJualTotal: 50000m,
            PakaiBrgId: "PKB-01",
            TrsBillingId: "TBILL-BRG-01"
        );

        tdkBarangRepoMock.Setup(x => x.ListByTindakanId(tdkId))
            .Returns([bomItem]);

        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == tdkId)))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        var sutWithBom = new TindakanVoidHandler(
            _tdkRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _trsBillingRepoMock.Object,
            _jurnalRepoMock.Object,
            _billingLockValidatorMock.Object,
            tdkBarangRepoMock.Object,
            tdkStokPendingRepoMock.Object,
            pakaiBrgInternalServiceMock.Object);

        var cmd = new TindakanVoidCmd(tdkId, "SUPERVISOR", "Salah input tindakan dengan BOM", "127.0.0.1", "Mozilla/5.0");

        // Act
        await sutWithBom.Handle(cmd, CancellationToken.None);

        // Assert
        tdk.AuditTrail.IsVoided.Should().BeTrue();
        tdkBarangRepoMock.Verify(x => x.MarkVoid(tdkId, "SUPERVISOR"), Times.Once);
        tdkStokPendingRepoMock.Verify(x => x.MarkVoidByTindakanId(tdkId, _testNow, "SUPERVISOR"), Times.Once);
        pakaiBrgInternalServiceMock.Verify(x => x.VoidFromTindakan(
            It.Is<PakaiBrgVoidFromTindakanCommand>(c => c.PakaiBrgId == "PKB-01" && c.UserId == "SUPERVISOR"),
            It.IsAny<CancellationToken>()), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "TBILL-BRG-01")), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == "TBILL-BRG-01")), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningNotFound_ShouldProceedGracefully()
    {
        // Arrange
        var tdk = CreateTestTindakan("TDK-001", "REG-MISSING");
        _tdkRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITindakanKey>()))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        var cmd = new TindakanVoidCmd("TDK-001", "USER01", "Alasan", "127.0.0.1", "Mozilla/5.0");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        tdk.AuditTrail.IsVoided.Should().BeTrue();
        _tdkRepoMock.Verify(x => x.SaveChanges(tdk), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }
}
