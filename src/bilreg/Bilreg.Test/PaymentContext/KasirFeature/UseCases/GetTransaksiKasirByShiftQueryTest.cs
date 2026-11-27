using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetTransaksiKasirByShiftQueryTest
{
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly GetTransaksiKasirByShiftHandler _sut;
    private readonly GetTransaksiKasirByShiftValidator _validator;

    public GetTransaksiKasirByShiftQueryTest()
    {
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _sut = new GetTransaksiKasirByShiftHandler(_trsKasirRepoMock.Object);
        _validator = new GetTransaksiKasirByShiftValidator();
    }

    [Fact]
    public async Task GivenShiftWithTransactions_WhenHandle_ThenReturnsAllOrderedDescendingByTglJamTrs()
    {
        // Arrange
        const string shiftId = "SH26000001";
        var t1 = TrsKasirModel.CreateTerimaKas(
            shiftId: shiftId,
            kasirId: "KASIR-01",
            regId: "RG26000001",
            pasienId: "RM0001",
            pasienName: "Pasien Pertama",
            orderRef: "ORD01",
            keterangan: "Pembayaran 1",
            totalNilai: 100_000m,
            payments: new[] { TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m, 0m) },
            trsTime: new DateTime(2026, 11, 25, 8, 0, 0));

        var t2 = TrsKasirModel.CreateTerimaKas(
            shiftId: shiftId,
            kasirId: "KASIR-01",
            regId: "RG26000002",
            pasienId: "RM0002",
            pasienName: "Pasien Void",
            orderRef: "ORD02",
            keterangan: "Pembayaran Void",
            totalNilai: 150_000m,
            payments: new[] { TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 150_000m, 0m) },
            trsTime: new DateTime(2026, 11, 25, 9, 30, 0));
        t2.Void("SPV01", "Salah input nominal", new DateTime(2026, 11, 25, 9, 35, 0));

        var t3 = TrsKasirModel.CreateKeluarKas(
            shiftId: shiftId,
            kasirId: "KASIR-01",
            regId: "RG26000003",
            pasienId: "RM0003",
            pasienName: "Pasien Ketiga",
            orderRef: "REF03",
            keterangan: "Refund",
            totalNilai: 50_000m,
            metodeBayar: KasirConst.MetodeBayarTunai,
            trsTime: new DateTime(2026, 11, 25, 10, 15, 0));

        _trsKasirRepoMock
            .Setup(x => x.ListByShift(shiftId))
            .Returns(new[] { t1, t2, t3 });

        var query = new GetTransaksiKasirByShiftQuery($" {shiftId} ");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Select(x => x.TrsKasirId).Should().ContainInOrder(t3.TrsKasirId, t2.TrsKasirId, t1.TrsKasirId);

        var newest = result[0];
        newest.TrsKasirId.Should().Be(t3.TrsKasirId);
        newest.TglTrs.Should().Be("2026-11-25");
        newest.JamTrs.Should().Be("10:15:00");
        newest.TglJamTrs.Should().Be("2026-11-25 10:15:00");
        newest.JenisTrs.Should().Be(KasirConst.JenisTrsKeluar);
        newest.RegId.Should().Be("RG26000003");
        newest.PasienId.Should().Be("RM0003");
        newest.PasienName.Should().Be("Pasien Ketiga");
        newest.OrderRef.Should().Be("REF03");
        newest.Keterangan.Should().Be("Refund");
        newest.TotalNilai.Should().Be(50_000m);
        newest.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        newest.IsVoid.Should().BeFalse();

        var voided = result[1];
        voided.TrsKasirId.Should().Be(t2.TrsKasirId);
        voided.StatusTrs.Should().Be(KasirConst.StatusTrsVoid);
        voided.IsVoid.Should().BeTrue();

        _trsKasirRepoMock.Verify(x => x.ListByShift(shiftId), Times.Once);
    }

    [Fact]
    public async Task GivenShiftWithNoTransactions_WhenHandle_ThenReturnsEmptyList()
    {
        // Arrange
        const string shiftId = "SH26000099";
        _trsKasirRepoMock
            .Setup(x => x.ListByShift(shiftId))
            .Returns(Array.Empty<TrsKasirModel>());

        var query = new GetTransaksiKasirByShiftQuery(shiftId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
        _trsKasirRepoMock.Verify(x => x.ListByShift(shiftId), Times.Once);
    }

    [Fact]
    public async Task GivenRepoReturnsNull_WhenHandle_ThenReturnsEmptyList()
    {
        // Arrange
        const string shiftId = "SH26000098";
        _trsKasirRepoMock
            .Setup(x => x.ListByShift(shiftId))
            .Returns((IEnumerable<TrsKasirModel>)null!);

        var query = new GetTransaksiKasirByShiftQuery(shiftId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GivenNullQuery_WhenHandle_ThenThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenEmptyShiftId_WhenHandle_ThenThrowsArgumentException(string? invalidShiftId)
    {
        // Arrange
        var query = new GetTransaksiKasirByShiftQuery(invalidShiftId!);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _trsKasirRepoMock.Verify(x => x.ListByShift(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GivenValidShiftId_WhenValidate_ThenPasses()
    {
        var query = new GetTransaksiKasirByShiftQuery("SH26000001");
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GivenEmptyShiftId_WhenValidate_ThenFails(string? invalidShiftId)
    {
        var query = new GetTransaksiKasirByShiftQuery(invalidShiftId!);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetTransaksiKasirByShiftQuery.ShiftId));
    }
}
