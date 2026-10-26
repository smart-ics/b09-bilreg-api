using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositVoidHandlerTest
{
    private readonly Mock<IDepositRepo> _depositRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly DepositVoidHandler _sut;

    public DepositVoidHandlerTest()
    {
        _depositRepoMock = new Mock<IDepositRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _sut = new DepositVoidHandler(_depositRepoMock.Object, _tglJamProviderMock.Object);
    }

    private static DepositModel CreateFakeDeposit(string depositId, string regId, decimal nilai, bool isVoided = false)
    {
        var audit = new AuditTrailType(
            new AuditInfoType("USER01", new DateTime(2026, 10, 22, 10, 0, 0)),
            new AuditInfoType("USER01", new DateTime(2026, 10, 22, 10, 0, 0)),
            AuditInfoType.Default);

        if (isVoided)
        {
            audit.Batal("USER_PREV_VOID", new DateTime(2026, 10, 22, 11, 0, 0));
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
    public async Task GivenInvalidDepositId_WhenHandle_ThenThrowsArgumentException(string? invalidDepositId)
    {
        // Arrange
        var cmd = new DepositVoidCmd(invalidDepositId!, "USER01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenInvalidUserId_WhenHandle_ThenThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        var cmd = new DepositVoidCmd("DP01", invalidUserId!);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenDepositNotFound_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new DepositVoidCmd("DP99", "USER01");
        _depositRepoMock.Setup(x => x.LoadEntity(It.Is<IDepositId>(k => k.DepositId == "DP99")))
            .Returns(MayBe<DepositModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Deposit DP99 tidak ditemukan.*");
    }

    [Fact]
    public async Task GivenAlreadyVoidedDeposit_WhenHandle_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var deposit = CreateFakeDeposit("DP01", "REG01", 500_000, isVoided: true);
        var cmd = new DepositVoidCmd("DP01", "USER01");
        _depositRepoMock.Setup(x => x.LoadEntity(It.Is<IDepositId>(k => k.DepositId == "DP01")))
            .Returns(MayBe.From(deposit));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Deposit DP01 sudah dibatalkan.*");
        _depositRepoMock.Verify(x => x.SaveChanges(It.IsAny<DepositModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenDepositWithAllocationUsage_WhenHandle_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var regId = "REG01";
        var deposit = CreateFakeDeposit("DP01", regId, 1_000_000, isVoided: false);
        var cmd = new DepositVoidCmd("DP01", "USER_VOID");

        _depositRepoMock.Setup(x => x.LoadEntity(It.Is<IDepositId>(k => k.DepositId == "DP01")))
            .Returns(MayBe.From(deposit));
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 1_000_000, 300_000, 700_000));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Deposit tidak dapat dibatalkan karena telah digunakan dalam alokasi pembayaran pada registrasi {regId}.*");
        _depositRepoMock.Verify(x => x.SaveChanges(It.IsAny<DepositModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenDepositWithoutAllocationUsage_WhenHandle_ThenVoidSucceedsAndSavesChanges()
    {
        // Arrange
        var regId = "REG01";
        var now = new DateTime(2026, 10, 22, 14, 30, 0);
        var deposit = CreateFakeDeposit("DP01", regId, 1_000_000, isVoided: false);
        var cmd = new DepositVoidCmd("DP01", "USER_VOID");

        _tglJamProviderMock.Setup(x => x.Now).Returns(now);
        _depositRepoMock.Setup(x => x.LoadEntity(It.Is<IDepositId>(k => k.DepositId == "DP01")))
            .Returns(MayBe.From(deposit));
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(new DepositSaldoModel(regId, 1_000_000, 0, 1_000_000));

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        deposit.Audit.IsVoided.Should().BeTrue();
        deposit.Audit.Voided.UserId.Should().Be("USER_VOID");
        deposit.Audit.Voided.Timestamp.Should().Be(now);
        _depositRepoMock.Verify(x => x.SaveChanges(deposit), Times.Once);
    }

    [Fact]
    public async Task GivenNullSaldoFromRepo_WhenHandle_ThenVoidSucceedsAndSavesChanges()
    {
        // Arrange
        var regId = "REG01";
        var now = new DateTime(2026, 10, 22, 14, 30, 0);
        var deposit = CreateFakeDeposit("DP01", regId, 500_000, isVoided: false);
        var cmd = new DepositVoidCmd("DP01", "USER_VOID");

        _tglJamProviderMock.Setup(x => x.Now).Returns(now);
        _depositRepoMock.Setup(x => x.LoadEntity(It.Is<IDepositId>(k => k.DepositId == "DP01")))
            .Returns(MayBe.From(deposit));
        _depositRepoMock.Setup(x => x.GetSaldo(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns((DepositSaldoModel)null!);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        deposit.Audit.IsVoided.Should().BeTrue();
        deposit.Audit.Voided.UserId.Should().Be("USER_VOID");
        deposit.Audit.Voided.Timestamp.Should().Be(now);
        _depositRepoMock.Verify(x => x.SaveChanges(deposit), Times.Once);
    }
}
