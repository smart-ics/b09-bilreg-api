using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetKasirShiftActiveQueryTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly GetKasirShiftActiveHandler _sut;
    private readonly GetKasirShiftActiveValidator _validator;

    private static readonly DateTime OpenTime = new(2026, 11, 7, 7, 0, 0);

    public GetKasirShiftActiveQueryTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();

        _sut = new GetKasirShiftActiveHandler(
            _kasirShiftRepoMock.Object,
            _trsKasirRepoMock.Object);
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

        // Transactions:
        // Terima BERHASIL: 300k
        // Terima VOID: 50k (ignored)
        // Keluar BERHASIL: 50k
        // Keluar VOID: 20k (ignored)
        var pTunai300 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 300_000m, 0m);
        var trsTerima = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG01", "P01", "Pasien A", "ORD01", "Ket", 300_000m, new[] { pTunai300 }, OpenTime.AddHours(1));

        var pTunai50 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 50_000m, 0m);
        var trsTerimaVoid = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG02", "P02", "Pasien B", "ORD02", "Ket", 50_000m, new[] { pTunai50 }, OpenTime.AddHours(2));
        trsTerimaVoid.Void("KASIR-01", "Batal", OpenTime.AddHours(2));

        var pTunaiKeluar50 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 50_000m, 0m);
        var trsKeluar = TrsKasirModel.CreateKeluarKas(shift.ShiftId, "KASIR-01", "REG01", "P01", "Pasien A", "ORD01", "Refund", 50_000m, new[] { pTunaiKeluar50 }, OpenTime.AddHours(3));

        var pTunaiKeluar20 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 20_000m, 0m);
        var trsKeluarVoid = TrsKasirModel.CreateKeluarKas(shift.ShiftId, "KASIR-01", "REG02", "P02", "Pasien B", "ORD02", "Refund", 20_000m, new[] { pTunaiKeluar20 }, OpenTime.AddHours(4));
        trsKeluarVoid.Void("KASIR-01", "Batal", OpenTime.AddHours(4));

        _trsKasirRepoMock
            .Setup(x => x.ListByShift(shift.ShiftId))
            .Returns(new[] { trsTerima, trsTerimaVoid, trsKeluar, trsKeluarVoid });

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
        _trsKasirRepoMock.Verify(x => x.ListByShift(It.IsAny<string>()), Times.Never);
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
