using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.BillingLockFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.Shared.BillingLockFeature;

public class BillingLockValidationServiceTest
{
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly BillingLockValidationService _sut;

    public BillingLockValidationServiceTest()
    {
        _sut = new BillingLockValidationService(_tataRekeningRepoMock.Object);
    }

    [Fact]
    public void EnsureCanModifyBill_WhenStatusOpened_ShouldNotThrow()
    {
        // Arrange
        var tataRekening = TataRekeningModel.Create("REG01");

        // Act
        var act = () => tataRekening.EnsureCanModifyBill();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanModifyBill_WhenStatusClosed_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        var tataRekening = TataRekeningModel.Create("REG01");
        tataRekening.Close();

        // Act
        var act = () => tataRekening.EnsureCanModifyBill();

        // Assert
        act.Should().Throw<BillingLockViolationException>()
            .Where(e => e.RegId == "REG01" && e.Status == TataRekeningStatusEnum.Closed)
            .WithMessage("*Episode billing 'REG01' telah berstatus Closed*");
    }

    [Fact]
    public void EnsureCanModifyBill_WhenStatusFinalized_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        var tataRekening = new TataRekeningModel(
            "REG01",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("USER01", DateTime.Now),
            [],
            []);

        // Act
        var act = () => tataRekening.EnsureCanModifyBill();

        // Assert
        act.Should().Throw<BillingLockViolationException>()
            .Where(e => e.RegId == "REG01" && e.Status == TataRekeningStatusEnum.Finalized)
            .WithMessage("*Episode billing 'REG01' telah berstatus Finalized*");
    }

    [Fact]
    public void EnsureCanModifyBill_WhenStatusLunas_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        var tataRekening = new TataRekeningModel(
            "REG01",
            TataRekeningStatusEnum.Lunas,
            new TataRekeningFinalizationType("USER01", DateTime.Now),
            [],
            []);

        // Act
        var act = () => tataRekening.EnsureCanModifyBill();

        // Assert
        act.Should().Throw<BillingLockViolationException>()
            .Where(e => e.RegId == "REG01" && e.Status == TataRekeningStatusEnum.Lunas)
            .WithMessage("*Episode billing 'REG01' telah berstatus Lunas*");
    }

    [Fact]
    public async Task EnsureCanModifyBillingAsync_WhenRegIdEmpty_ShouldReturnCompletedTask()
    {
        // Act
        var act = async () => await _sut.EnsureCanModifyBillingAsync("");

        // Assert
        await act.Should().NotThrowAsync();
        _tataRekeningRepoMock.Verify(x => x.LoadEntity(It.IsAny<IRegKey>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCanModifyBillingAsync_WhenTataRekeningNotFound_ShouldNotThrow()
    {
        // Arrange
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-NONE")))
            .Returns(MayBe<TataRekeningModel>.None);

        // Act
        var act = async () => await _sut.EnsureCanModifyBillingAsync("REG-NONE");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanModifyBillingAsync_WhenTataRekeningOpened_ShouldNotThrow()
    {
        // Arrange
        var tataRekening = TataRekeningModel.Create("REG01");
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG01")))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = async () => await _sut.EnsureCanModifyBillingAsync("REG01");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanModifyBillingAsync_WhenTataRekeningFinalized_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        var tataRekening = new TataRekeningModel(
            "REG-FINAL",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("VERIF", DateTime.Now),
            [],
            []);
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-FINAL")))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = async () => await _sut.EnsureCanModifyBillingAsync("REG-FINAL");

        // Assert
        await act.Should().ThrowAsync<BillingLockViolationException>()
            .WithMessage("*Episode billing 'REG-FINAL' telah berstatus Finalized*");
    }

    [Fact]
    public async Task EnsureCanModifyBillingAsync_WhenTataRekeningClosed_ShouldThrowBillingLockViolationException()
    {
        // Arrange
        var tataRekening = TataRekeningModel.Create("REG-CLOSED");
        tataRekening.Close();
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-CLOSED")))
            .Returns(MayBe.From(tataRekening));

        // Act
        var act = async () => await _sut.EnsureCanModifyBillingAsync("REG-CLOSED");

        // Assert
        await act.Should().ThrowAsync<BillingLockViolationException>()
            .WithMessage("*Episode billing 'REG-CLOSED' telah berstatus Closed*");
    }
}
