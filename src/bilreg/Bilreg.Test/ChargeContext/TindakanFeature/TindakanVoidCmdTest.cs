using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
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

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanVoidCmdTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<IBillingLockValidationService> _billingLockValidatorMock = new();

    private readonly TindakanVoidHandler _sut;
    private readonly DateTime _testNow = new(2026, 10, 20, 11, 0, 0);

    public TindakanVoidCmdTest()
    {
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
    public async Task Handle_WhenTataRekeningNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var tdk = CreateTestTindakan("TDK-001", "REG-MISSING");
        _tdkRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITindakanKey>()))
            .Returns(MayBe.From(tdk));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        var cmd = new TindakanVoidCmd("TDK-001", "USER01", "Alasan", "127.0.0.1", "Mozilla/5.0");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tata Rekening 'REG-MISSING' not found*");
    }
}
