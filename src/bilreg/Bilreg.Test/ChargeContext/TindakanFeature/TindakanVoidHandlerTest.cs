using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
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

public class TindakanVoidHandlerTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly TindakanVoidHandler _sut;

    public TindakanVoidHandlerTest()
    {
        _sut = new TindakanVoidHandler(
            _tdkRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _trsBillingRepoMock.Object,
            _jurnalRepoMock.Object,
            _tataRekeningRepoMock.Object);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 16, 10, 0, 0));
    }

    private static TindakanModel CreateTindakan(string tindakanId, string regId) =>
        new(
            tindakanId,
            new DateTime(2026, 10, 16, 9, 0, 0),
            orderTindakanId: string.Empty,
            new RegReff(regId, "PAS-01", "Pasien Test"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [],
            AuditTrailType.Default);

    [Fact]
    public async Task GivenBillingInOpenedStatus_WhenVoid_ThenSucceedsAndBillDeletedFromTataRekening()
    {
        // Arrange
        const string tindakanId = "TDK-01";
        const string regId = "REG-01";
        var cmd = new TindakanVoidCmd(tindakanId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var tindakan = CreateTindakan(tindakanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tindakanId, regId, 50000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Opened, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(cmd))
            .Returns(MayBe.From(tindakan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        tataRekening.ListTrsBill.Should().NotContain(x => x.TrsBillingId == tindakanId);
        tindakan.AuditTrail.IsVoided.Should().BeTrue();

        _tdkRepoMock.Verify(x => x.SaveChanges(tindakan), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == tindakanId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == tindakanId)), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task GivenBillingInFinalizedStatus_WhenVoid_ThenThrowsInvalidOperationExceptionAndBubbles()
    {
        // Arrange
        const string tindakanId = "TDK-01";
        const string regId = "REG-01";
        var cmd = new TindakanVoidCmd(tindakanId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var tindakan = CreateTindakan(tindakanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tindakanId, regId, 50000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Finalized, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(cmd))
            .Returns(MayBe.From(tindakan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");

        _tdkRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GivenBillingInLunasStatus_WhenVoid_ThenThrowsInvalidOperationExceptionAndBubbles()
    {
        // Arrange
        const string tindakanId = "TDK-01";
        const string regId = "REG-01";
        var cmd = new TindakanVoidCmd(tindakanId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var tindakan = CreateTindakan(tindakanId, regId);
        var bill = TataRekeningTestDataBuilder.CreateBill(tindakanId, regId, 50000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(regId, TataRekeningStatusEnum.Lunas, bill);

        _tdkRepoMock.Setup(x => x.LoadEntity(cmd))
            .Returns(MayBe.From(tindakan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _tdkRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GivenTataRekeningNotFound_WhenVoid_ThenProceedsGracefullyWithoutSavingTataRekening()
    {
        // Arrange
        const string tindakanId = "TDK-01";
        const string regId = "REG-01";
        var cmd = new TindakanVoidCmd(tindakanId, "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        var tindakan = CreateTindakan(tindakanId, regId);

        _tdkRepoMock.Setup(x => x.LoadEntity(cmd))
            .Returns(MayBe.From(tindakan));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        tindakan.AuditTrail.IsVoided.Should().BeTrue();
        _tdkRepoMock.Verify(x => x.SaveChanges(tindakan), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == tindakanId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == tindakanId)), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task GivenTindakanNotFound_WhenVoid_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new TindakanVoidCmd("TDK-NONEXISTENT", "USER-01", "Salah input", "127.0.0.1", "TestAgent");
        _tdkRepoMock.Setup(x => x.LoadEntity(cmd))
            .Returns(MayBe<TindakanModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*TDK-NONEXISTENT*");

        _tataRekeningRepoMock.Verify(x => x.LoadEntity(It.IsAny<IRegKey>()), Times.Never);
    }
}
