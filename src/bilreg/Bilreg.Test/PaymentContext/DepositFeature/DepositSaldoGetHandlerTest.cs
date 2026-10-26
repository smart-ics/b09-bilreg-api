using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositSaldoGetHandlerTest
{
    private readonly Mock<IDepositRepo> _depositRepoMock;
    private readonly DepositSaldoGetHandler _sut;

    public DepositSaldoGetHandlerTest()
    {
        _depositRepoMock = new Mock<IDepositRepo>();
        _sut = new DepositSaldoGetHandler(_depositRepoMock.Object);
    }

    private static DepositModel CreateFakeDeposit(string depositId, string regId, decimal nilai, bool isVoided = false)
    {
        var audit = new AuditTrailType(
            new AuditInfoType("USER01", new DateTime(2026, 10, 22, 10, 0, 0)),
            new AuditInfoType("USER01", new DateTime(2026, 10, 22, 10, 0, 0)),
            AuditInfoType.Default);

        if (isVoided)
        {
            audit.Batal("USER_VOID", new DateTime(2026, 10, 22, 12, 0, 0));
        }

        return new DepositModel(
            depositId: depositId,
            depositDate: new DateTime(2026, 10, 22, 10, 0, 0),
            reg: new RegReff(regId, "MR01", "Pasien A"),
            layanan: new LayananReff("LAY01", "Layanan A"),
            ketarangan: "Deposit Test",
            nilaiDeposit: nilai,
            audit: audit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenInvalidRegId_WhenHandle_ThenThrowsArgumentException(string? invalidRegId)
    {
        // Arrange
        var qry = new DepositSaldoGetQry(invalidRegId!);

        // Act
        var act = () => _sut.Handle(qry, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenNullSaldoAndNullDepositsFromRepo_WhenHandle_ThenReturnsDefaultZeroAndStatusTidakAdaDeposit()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns((DepositSaldoModel)null!);
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns((IEnumerable<DepositModel>)null!);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(0);
        result.TotalTerpakai.Should().Be(0);
        result.SaldoAktif.Should().Be(0);
        result.Status.Should().Be("Tidak Ada Deposit");
    }

    [Fact]
    public async Task GivenNoDeposits_WhenHandle_ThenReturnsStatusTidakAdaDeposit()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 0, 0, 0));
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns([]);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(0);
        result.TotalTerpakai.Should().Be(0);
        result.SaldoAktif.Should().Be(0);
        result.Status.Should().Be("Tidak Ada Deposit");
    }

    [Fact]
    public async Task GivenActiveDepositWithRemainingSaldo_WhenHandle_ThenReturnsStatusAktif()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        var deposits = new List<DepositModel>
        {
            CreateFakeDeposit("DP01", regId, 1_000_000m, isVoided: false)
        };
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 1_000_000m, 400_000m, 600_000m));
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(deposits);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(1_000_000m);
        result.TotalTerpakai.Should().Be(400_000m);
        result.SaldoAktif.Should().Be(600_000m);
        result.Status.Should().Be("Aktif");
    }

    [Fact]
    public async Task GivenFullyUsedDeposit_WhenHandle_ThenReturnsStatusHabisTerpakai()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        var deposits = new List<DepositModel>
        {
            CreateFakeDeposit("DP01", regId, 1_000_000m, isVoided: false)
        };
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 1_000_000m, 1_000_000m, 0));
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(deposits);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(1_000_000m);
        result.TotalTerpakai.Should().Be(1_000_000m);
        result.SaldoAktif.Should().Be(0);
        result.Status.Should().Be("Habis Terpakai");
    }

    [Fact]
    public async Task GivenAllDepositsVoided_WhenHandle_ThenReturnsStatusDibatalkan()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        var deposits = new List<DepositModel>
        {
            CreateFakeDeposit("DP01", regId, 500_000m, isVoided: true),
            CreateFakeDeposit("DP02", regId, 500_000m, isVoided: true)
        };
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 0, 0, 0));
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(deposits);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(0);
        result.TotalTerpakai.Should().Be(0);
        result.SaldoAktif.Should().Be(0);
        result.Status.Should().Be("Dibatalkan");
    }

    [Fact]
    public async Task GivenMultipleDepositsWithOneVoidedAndOneActive_WhenHandle_ThenReturnsStatusAktif()
    {
        // Arrange
        var regId = "REG01";
        var qry = new DepositSaldoGetQry(regId);
        var deposits = new List<DepositModel>
        {
            CreateFakeDeposit("DP01", regId, 500_000m, isVoided: true),
            CreateFakeDeposit("DP02", regId, 750_000m, isVoided: false)
        };
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 750_000m, 250_000m, 500_000m));
        _depositRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(deposits);

        // Act
        var result = await _sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be(regId);
        result.TotalSetoran.Should().Be(750_000m);
        result.TotalTerpakai.Should().Be(250_000m);
        result.SaldoAktif.Should().Be(500_000m);
        result.Status.Should().Be("Aktif");
    }
}
