using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedReleaseCommandTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly PakaiBedReleaseCommandHandler _sut;

    public PakaiBedReleaseCommandTest()
    {
        _sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static PakaiBedModel CreateTestBed(string pakaiBedId = "PKB001")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff("REG001", "PAS001", "Pasien A");
        var layanan = new LayananReff("LAY01", "Ranap Mawar");
        var bed = new BedReff("BED01", "Bed 01", true);
        var tipeKamar = new TipeKamarReff("TK01", "Standar", true);
        var kelas = new KelasReff("KL01", "Kelas 1");
        var auditTrail = new AuditTrailType(masuk, AuditInfoType.Default, AuditInfoType.Default);

        return new PakaiBedModel(
            pakaiBedId,
            periode,
            reg,
            layanan,
            bed,
            tipeKamar,
            kelas,
            300000m,
            auditTrail);
    }

    [Fact]
    public async Task Handle_WhenBedFound_ReleasesBedAndDispatchesReconcileCheckoutCmd()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Discharged");

        var reconcileResponse = new RoomChargeReconcileResponse("RC001", 1.0m, 300000m);
        RoomChargeReconcileCheckoutCmd? capturedReconcileCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeReconcileResponse>, CancellationToken>((req, _) => capturedReconcileCmd = (RoomChargeReconcileCheckoutCmd)req)
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be("PKB001");
        result.TglJamOut.Should().Be(checkoutTime);
        result.ReconcileResult.Should().Be(reconcileResponse);

        // Verify PakaiBed was released and saved
        bed.Periode.Keluar.UserId.Should().Be("nurse_user");
        bed.Periode.Keluar.Timestamp.Should().Be(checkoutTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(bed), Times.Once);

        // Verify RoomChargeReconcileCheckoutCmd was dispatched via mediator
        capturedReconcileCmd.Should().NotBeNull();
        capturedReconcileCmd!.PakaiBedId.Should().Be("PKB001");
        capturedReconcileCmd.CheckOutTime.Should().Be(checkoutTime);
        capturedReconcileCmd.UserId.Should().Be("nurse_user");
    }

    [Fact]
    public async Task Handle_WhenTglJamOutDefault_UsesTglJamProviderNow()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var now = new DateTime(2026, 10, 19, 15, 30, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var cmd = new PakaiBedReleaseCommand("PKB001", default, "nurse_user");

        var reconcileResponse = new RoomChargeReconcileResponse("RC002", 0.5m, 150000m);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TglJamOut.Should().Be(now);
        bed.Periode.Keluar.Timestamp.Should().Be(now);
        _mediatorMock.Verify(x => x.Send(
            It.Is<RoomChargeReconcileCheckoutCmd>(c => c.CheckOutTime == now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new PakaiBedReleaseCommand("NONEXISTENT", DateTime.UtcNow, "nurse1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NONEXISTENT*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPakaiBedIdEmpty_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("", DateTime.UtcNow, "nurse1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenUserIdEmpty_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB001", DateTime.UtcNow, "");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
