using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepVoidHandlerTest
{
    private readonly Mock<IResepRepo> _resepRepo = new();
    private readonly Mock<IResepVoidDal> _resepDal = new();
    private readonly ResepVoidHandler _sut;

    public ResepVoidHandlerTest()
    {
        _sut = new ResepVoidHandler(_resepRepo.Object, _resepDal.Object);
    }

    private static ResepModel CreateValidResep(string resepId = "KP12345")
    {
        var reg = RegModel.Default;
        var bodyMetric = new BodyMetricType(65m, 170m, 1.75m);
        var dokter = DokterType.Default;
        var layanan = LayananType.Default;
        var urgenitas = UrgenitasType.Default;
        var tipeBrg = TipeBrgType.Default;

        var resep = ResepModel.Create(
            reg,
            bodyMetric,
            dokter,
            layanan,
            urgenitas,
            tipeBrg,
            0,
            "Resep Rawat Jalan",
            "USER01");

        var brg = BrgObatType.Default with { BrgId = "BRG01", BrgName = "Amoxicillin 500mg" };
        var satuan = SatuanType.Create("TAB", "Tablet");
        var etiket = EtiketType.Create("3x1", "Sesudah makan");
        resep.AddObat(brg, satuan, 10m, 0, etiket);
        return resep;
    }

    [Fact]
    public async Task Handle_ResepNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new ResepVoidCommand("KP99999", "USR01", "Pasien membatalkan periksa");
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP99999")))
            .Returns(MayBe<ResepModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*KP99999*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResepAlreadyVoid_ThrowsInvalidOperationException()
    {
        // Arrange
        var resep = CreateValidResep();
        resep.Void("PREV_USER", "Sudah dibatalkan sebelumnya", DateTime.Now);

        var cmd = new ResepVoidCommand(resep.ResepId, "USR02", "Alasan baru");
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == resep.ResepId)))
            .Returns(MayBe.From(resep));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*berstatus void*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResepAlreadyProcessedInPharmacy_ThrowsInvalidOperationException()
    {
        // Arrange
        var resep = CreateValidResep();
        var cmd = new ResepVoidCommand(resep.ResepId, "USR02", "Pasien pulang paksa");

        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == resep.ResepId)))
            .Returns(MayBe.From(resep));
        _resepDal.Setup(x => x.HasBeenProcessed(resep.ResepId))
            .Returns(true);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*diproses/didispensasi farmasi*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
        resep.IsVoid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRequest_Success_VoidsResepAndSavesChanges()
    {
        // Arrange
        var resep = CreateValidResep();
        var cmd = new ResepVoidCommand(resep.ResepId, "USR02", "Koreksi dosis oleh dokter");

        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == resep.ResepId)))
            .Returns(MayBe.From(resep));
        _resepDal.Setup(x => x.HasBeenProcessed(resep.ResepId))
            .Returns(false);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ResepId.Should().Be(resep.ResepId);
        result.Status.Should().Be("Voided");

        resep.IsVoid.Should().BeTrue();
        resep.VoidReason.Should().Be("Koreksi dosis oleh dokter");
        resep.AuditTrail.Voided.UserId.Should().Be("USR02");

        _resepRepo.Verify(x => x.SaveChanges(resep), Times.Once);
    }

    [Theory]
    [InlineData(null, "USR01", "Alasan void")]
    [InlineData("", "USR01", "Alasan void")]
    [InlineData("   ", "USR01", "Alasan void")]
    public async Task Handle_InvalidResepId_ThrowsArgumentException(string? resepId, string userId, string voidReason)
    {
        // Arrange
        var cmd = new ResepVoidCommand(resepId!, userId, voidReason);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _resepRepo.Verify(x => x.LoadEntity(It.IsAny<IResepKey>()), Times.Never);
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Theory]
    [InlineData("KP123", null, "Alasan void")]
    [InlineData("KP123", "", "Alasan void")]
    [InlineData("KP123", "   ", "Alasan void")]
    public async Task Handle_InvalidUserId_ThrowsArgumentException(string resepId, string? userId, string voidReason)
    {
        // Arrange
        var cmd = new ResepVoidCommand(resepId, userId!, voidReason);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _resepRepo.Verify(x => x.LoadEntity(It.IsAny<IResepKey>()), Times.Never);
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Theory]
    [InlineData("KP123", "USR01", null)]
    [InlineData("KP123", "USR01", "")]
    [InlineData("KP123", "USR01", "   ")]
    public async Task Handle_InvalidVoidReason_ThrowsArgumentException(string resepId, string userId, string? voidReason)
    {
        // Arrange
        var cmd = new ResepVoidCommand(resepId, userId, voidReason!);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _resepRepo.Verify(x => x.LoadEntity(It.IsAny<IResepKey>()), Times.Never);
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }
}
