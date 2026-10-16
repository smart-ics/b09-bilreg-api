using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedBillingLockCheckerTest
{
    private readonly Mock<IPakaiBedBillingLockCheckerDal> _dalMock = new();
    private readonly PakaiBedBillingLockChecker _sut;

    public PakaiBedBillingLockCheckerTest()
    {
        _sut = new PakaiBedBillingLockChecker(_dalMock.Object);
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenHasRoomCharge_ReturnsLockedWithRoomChargeReason()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync("PB-101", "RG-202", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingLockStatusRaw(HasRoomCharge: true, HasCashierPayment: false, IsTataRekeningLocked: false));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeTrue();
        result.Reason.Should().Contain("ta_trs_roomcharge");
        result.Reason.Should().Contain("PB-101");
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenHasCashierPayment_ReturnsLockedWithCashierReason()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync("PB-101", "RG-202", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingLockStatusRaw(HasRoomCharge: false, HasCashierPayment: true, IsTataRekeningLocked: false));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeTrue();
        result.Reason.Should().Contain("ta_trs_billing2");
        result.Reason.Should().Contain("RG-202");
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenIsTataRekeningLocked_ReturnsLockedWithTataRekeningReason()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync("PB-101", "RG-202", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingLockStatusRaw(HasRoomCharge: false, HasCashierPayment: false, IsTataRekeningLocked: true));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeTrue();
        result.Reason.Should().Contain("BILRG_TataRekening");
        result.Reason.Should().Contain("RG-202");
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenMultipleLocksPresent_PrioritizesRoomChargeReason()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync("PB-101", "RG-202", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingLockStatusRaw(HasRoomCharge: true, HasCashierPayment: true, IsTataRekeningLocked: true));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeTrue();
        result.Reason.Should().Contain("ta_trs_roomcharge");
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenNoLocks_ReturnsUnlocked()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync("PB-101", "RG-202", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingLockStatusRaw(HasRoomCharge: false, HasCashierPayment: false, IsTataRekeningLocked: false));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeFalse();
        result.Reason.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenDalThrowsException_ReturnsLockedUnderFailClosedPrinciple()
    {
        // Arrange
        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated SQL connection failure"));

        // Act
        var result = await _sut.CheckLockStatusAsync("PB-101", "RG-202");

        // Assert
        result.IsLocked.Should().BeTrue();
        result.Reason.Should().Contain("Fail-Closed");
        result.Reason.Should().Contain("Simulated SQL connection failure");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckLockStatusAsync_WhenPakaiBedIdIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidPakaiBedId)
    {
        // Act
        var act = () => _sut.CheckLockStatusAsync(invalidPakaiBedId!, "RG-202");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckLockStatusAsync_WhenRegIdIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidRegId)
    {
        // Act
        var act = () => _sut.CheckLockStatusAsync("PB-101", invalidRegId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CheckLockStatusAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _dalMock
            .Setup(x => x.CheckBillingLockRawAsync(It.IsAny<string>(), It.IsAny<string>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act
        var act = () => _sut.CheckLockStatusAsync("PB-101", "RG-202", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CheckBillingLockSql_QueriesExpectedTablesAndParameters()
    {
        // Assert
        var sql = PakaiBedBillingLockCheckerDal.CheckBillingLockSql;

        sql.Should().Contain("ta_trs_roomcharge");
        sql.Should().Contain("ta_trs_billing2");
        sql.Should().Contain("ta_trs_billing");
        sql.Should().Contain("BILRG_TataRekening");
        sql.Should().Contain("@PakaiBedId");
        sql.Should().Contain("@RegId");
    }

    [Fact]
    public void AddPakaiBedFeatureInfrastructure_RegistersExpectedDependenciesWithScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPakaiBedFeatureInfrastructure();

        // Assert
        services.Should().Contain(d =>
            d.ServiceType == typeof(IPakaiBedBillingLockChecker) &&
            d.ImplementationType == typeof(PakaiBedBillingLockChecker) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPakaiBedBillingLockCheckerDal) &&
            d.ImplementationType == typeof(PakaiBedBillingLockCheckerDal) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPakaiBedFeatureInfrastructure_ResolvesCleanlyWithStrictValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<DatabaseOptions>(opt =>
        {
            opt.ServerName = "localhost";
            opt.DbName = "db";
        });
        services.AddPakaiBedFeatureInfrastructure();

        // Act
        using var rootProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = rootProvider.CreateScope();
        var provider = scope.ServiceProvider;
        var checker = provider.GetRequiredService<IPakaiBedBillingLockChecker>();
        var dal = provider.GetRequiredService<IPakaiBedBillingLockCheckerDal>();

        // Assert
        checker.Should().NotBeNull();
        checker.Should().BeOfType<PakaiBedBillingLockChecker>();
        dal.Should().NotBeNull();
        dal.Should().BeOfType<PakaiBedBillingLockCheckerDal>();
    }

    [Fact]
    public void BillingLockCheckResult_FactoryMethods_ProduceExpectedState()
    {
        // Act
        var unlocked = BillingLockCheckResult.Unlocked();
        var locked = BillingLockCheckResult.Locked("Test Reason");

        // Assert
        unlocked.IsLocked.Should().BeFalse();
        unlocked.Reason.Should().BeEmpty();

        locked.IsLocked.Should().BeTrue();
        locked.Reason.Should().Be("Test Reason");
    }
}
