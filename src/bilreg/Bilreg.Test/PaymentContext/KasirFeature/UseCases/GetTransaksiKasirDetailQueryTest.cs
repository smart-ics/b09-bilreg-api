using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetTransaksiKasirDetailQueryTest
{
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly GetTransaksiKasirDetailHandler _sut;
    private readonly GetTransaksiKasirDetailValidator _validator;

    private static readonly DateTime FixedNow = new(2026, 11, 7, 9, 30, 0);

    public GetTransaksiKasirDetailQueryTest()
    {
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _sut = new GetTransaksiKasirDetailHandler(_trsKasirRepoMock.Object);
        _validator = new GetTransaksiKasirDetailValidator();
    }

    [Fact]
    public async Task GivenExistingTrsKasirId_WhenHandle_ThenReturnsCompleteDetailResponse()
    {
        // Arrange
        var pTunai = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 150_000m, 50_000m);
        var pDebit = TrsKasirPaymentType.Create(KasirConst.MetodeBayarDebit, 200_000m, 0m, "12345678", "APPR001", "BCA");
        var model = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH26000001",
            kasirId: "KASIR-01",
            regId: "RG26000001",
            pasienId: "RM0001",
            pasienName: "Budi Santoso",
            orderRef: "BL26000001",
            keterangan: "Pembayaran rawat jalan",
            totalNilai: 300_000m,
            payments: new[] { pTunai, pDebit },
            trsTime: FixedNow);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var query = new GetTransaksiKasirDetailQuery(model.TrsKasirId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.TrsKasirId.Should().Be(model.TrsKasirId);
        result.TglTrs.Should().Be("2026-11-07");
        result.JamTrs.Should().Be("09:30:00");
        result.JenisTrs.Should().Be(KasirConst.JenisTrsTerima);
        result.ShiftId.Should().Be("SH26000001");
        result.KasirId.Should().Be("KASIR-01");
        result.RegId.Should().Be("RG26000001");
        result.PasienId.Should().Be("RM0001");
        result.PasienName.Should().Be("Budi Santoso");
        result.OrderRef.Should().Be("BL26000001");
        result.TotalNilai.Should().Be(300_000m);
        result.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        result.IsVoid.Should().BeFalse();

        result.Payments.Should().HaveCount(2);
        var pList = result.Payments.ToList();
        pList[0].MetodeBayar.Should().Be(KasirConst.MetodeBayarTunai);
        pList[0].NilaiBayar.Should().Be(150_000m);
        pList[0].NilaiKembalian.Should().Be(50_000m);
        pList[0].NilaiNetto.Should().Be(100_000m);

        pList[1].MetodeBayar.Should().Be(KasirConst.MetodeBayarDebit);
        pList[1].NilaiBayar.Should().Be(200_000m);
        pList[1].NomorKartu.Should().Be("12345678");
        pList[1].NomorReferensi.Should().Be("APPR001");
        pList[1].NamaBank.Should().Be("BCA");
    }

    [Fact]
    public async Task GivenNonExistentTrsKasirId_WhenHandle_ThenReturnsNull()
    {
        // Arrange
        _trsKasirRepoMock
            .Setup(x => x.GetKey("DX99999999"))
            .Returns((TrsKasirModel?)null);

        var query = new GetTransaksiKasirDetailQuery("DX99999999");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenValidQuery_WhenValidate_ThenPasses()
    {
        var query = new GetTransaksiKasirDetailQuery("DX26000001");
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GivenEmptyTrsKasirId_WhenValidate_ThenFails(string? id)
    {
        var query = new GetTransaksiKasirDetailQuery(id!);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrsKasirId");
    }
}
