using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetKasirShiftActiveQueryTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<IKasirShiftTotalsDal> _trsKasirDalMock;
    private readonly GetKasirShiftActiveHandler _sut;
    private readonly GetKasirShiftActiveValidator _validator;

    private static readonly DateTime OpenTime = new(2026, 11, 7, 7, 0, 0);

    public GetKasirShiftActiveQueryTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _trsKasirDalMock = new Mock<IKasirShiftTotalsDal>();

        _sut = new GetKasirShiftActiveHandler(
            _kasirShiftRepoMock.Object,
            _trsKasirDalMock.Object);
        _validator = new GetKasirShiftActiveValidator();
    }

    [Fact]
    public async Task GivenKasirWithActiveShift_WhenHandle_ThenReturnsActiveShiftWithAggregatedTotals()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 200_000m,
            openTime: OpenTime);

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns(shift);

        _trsKasirDalMock
            .Setup(x => x.GetShiftTotalsAsync(shift.ShiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KasirShiftTotalsDto(300_000m, 50_000m));

        var query = new GetKasirShiftActiveQuery("KASIR-01");

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response!.ShiftId.Should().Be(shift.ShiftId);
        response.TglShift.Should().Be("2026-11-07");
        response.KodeShift.Should().Be("1");
        response.KasirId.Should().Be("KASIR-01");
        response.Loket.Should().Be("Loket 01");
        response.TglJamBuka.Should().Be("2026-11-07 07:00:00");
        response.SaldoAwal.Should().Be(200_000m);
        response.TotalTerimaBerjalan.Should().Be(300_000m);
        response.TotalKeluarBerjalan.Should().Be(50_000m);
        response.SaldoKasBerjalan.Should().Be(450_000m); // 200k + 300k - 50k
        response.StatusShift.Should().Be(KasirConst.StatusShiftBuka);
    }

    [Theory]
    [InlineData(200_000, 0, 0, 200_000)]
    [InlineData(0, 500_000, 125_000, 375_000)]
    [InlineData(100_000, 150_000, 300_000, -50_000)]
    public async Task GivenVariousShiftTotals_WhenHandle_ThenCalculatesSaldoKasBerjalanCorrectly(
        decimal saldoAwal,
        decimal totalTerima,
        decimal totalKeluar,
        decimal expectedSaldoBerjalan)
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: saldoAwal,
            openTime: OpenTime);

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns(shift);

        _trsKasirDalMock
            .Setup(x => x.GetShiftTotalsAsync(shift.ShiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KasirShiftTotalsDto(totalTerima, totalKeluar));

        var query = new GetKasirShiftActiveQuery("KASIR-01");

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response!.SaldoAwal.Should().Be(saldoAwal);
        response.TotalTerimaBerjalan.Should().Be(totalTerima);
        response.TotalKeluarBerjalan.Should().Be(totalKeluar);
        response.SaldoKasBerjalan.Should().Be(expectedSaldoBerjalan);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenHandle_ThenForwardsCancellationTokenToTotalsDal()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 100_000m,
            openTime: OpenTime);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns(shift);

        _trsKasirDalMock
            .Setup(x => x.GetShiftTotalsAsync(shift.ShiftId, token))
            .ReturnsAsync(new KasirShiftTotalsDto(50_000m, 10_000m));

        var query = new GetKasirShiftActiveQuery("KASIR-01");

        // Act
        await _sut.Handle(query, token);

        // Assert
        _trsKasirDalMock.Verify(x => x.GetShiftTotalsAsync(shift.ShiftId, token), Times.Once);
    }

    [Fact]
    public async Task GivenCancelledToken_WhenTotalsDalThrowsOperationCanceled_ThenPropagatesException()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 100_000m,
            openTime: OpenTime);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns(shift);

        _trsKasirDalMock
            .Setup(x => x.GetShiftTotalsAsync(shift.ShiftId, token))
            .ThrowsAsync(new OperationCanceledException(token));

        var query = new GetKasirShiftActiveQuery("KASIR-01");

        // Act
        var act = () => _sut.Handle(query, token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GivenNullQuery_WhenHandle_ThenThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenKasirWithoutActiveShift_WhenHandle_ThenReturnsNull()
    {
        // Arrange
        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-99"))
            .Returns((KasirShiftModel?)null);

        var query = new GetKasirShiftActiveQuery("KASIR-99");

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().BeNull();
        _trsKasirDalMock.Verify(x => x.GetShiftTotalsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenEmptyKasirId_WhenHandle_ThenThrowsArgumentException(string? invalidKasirId)
    {
        // Arrange
        var query = new GetKasirShiftActiveQuery(invalidKasirId!);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Validator_WhenValidKasirId_ShouldPassValidation()
    {
        var query = new GetKasirShiftActiveQuery("KASIR-01");
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WhenEmptyKasirId_ShouldFailValidation(string invalidKasirId)
    {
        var query = new GetKasirShiftActiveQuery(invalidKasirId);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetKasirShiftActiveQuery.KasirId));
    }
}
