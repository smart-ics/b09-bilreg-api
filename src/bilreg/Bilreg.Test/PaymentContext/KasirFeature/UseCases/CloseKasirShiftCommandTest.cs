using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class CloseKasirShiftCommandTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly CloseKasirShiftHandler _sut;
    private readonly CloseKasirShiftValidator _validator;

    private static readonly DateTime OpenTime = new(2026, 11, 7, 7, 0, 0);
    private static readonly DateTime CloseTime = new(2026, 11, 7, 15, 0, 0);

    public CloseKasirShiftCommandTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(CloseTime);

        _sut = new CloseKasirShiftHandler(
            _kasirShiftRepoMock.Object,
            _trsKasirRepoMock.Object,
            _tglJamProviderMock.Object);
        _validator = new CloseKasirShiftValidator();
    }

    [Fact]
    public async Task GivenValidOpenShiftWithTransactions_WhenHandle_ThenAggregatesTotalsClosesShiftAndSavesToRepo()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 100_000m,
            openTime: OpenTime);

        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        // Transactions:
        // Terima BERHASIL: 100k + 50k = 150k Tunai
        // Terima BERHASIL Non-Tunai: 75k DEBIT (diabaikan dari rekonsiliasi kas tunai)
        // Terima VOID: 30k (ignored)
        // Keluar BERHASIL: 20k = 20k Tunai
        // Keluar BERHASIL Non-Tunai: 25k TRANSFER (diabaikan dari rekonsiliasi kas tunai)
        // Keluar VOID: 10k (ignored)
        var pTunai100 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m, 0m);
        var trsTerima1 = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG01", "P01", "Pasien A", "ORD01", "Keterangan", 100_000m, new[] { pTunai100 }, OpenTime.AddHours(1));

        var pTunai50 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 50_000m, 0m);
        var trsTerima2 = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG02", "P02", "Pasien B", "ORD02", "Keterangan", 50_000m, new[] { pTunai50 }, OpenTime.AddHours(2));

        var pDebit75 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarDebit, 75_000m, 0m);
        var trsTerimaDebit = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG04", "P04", "Pasien D", "ORD04", "Non Tunai", 75_000m, new[] { pDebit75 }, OpenTime.AddHours(2));

        var pTunai30 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 30_000m, 0m);
        var trsTerimaVoid = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG03", "P03", "Pasien C", "ORD03", "Keterangan", 30_000m, new[] { pTunai30 }, OpenTime.AddHours(3));
        trsTerimaVoid.Void("KASIR-01", "Batal transaksi", OpenTime.AddHours(3));

        var pTunai20 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 20_000m, 0m);
        var trsKeluar1 = TrsKasirModel.CreateKeluarKas(shift.ShiftId, "KASIR-01", "REG01", "P01", "Pasien A", "ORD01", "Refund", 20_000m, new[] { pTunai20 }, OpenTime.AddHours(4));

        var pTransfer25 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTransfer, 25_000m, 0m);
        var trsKeluarTransfer = TrsKasirModel.CreateKeluarKas(shift.ShiftId, "KASIR-01", "REG05", "P05", "Pasien E", "ORD05", "Refund Non Tunai", 25_000m, new[] { pTransfer25 }, OpenTime.AddHours(4));

        var pTunai10 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 10_000m, 0m);
        var trsKeluarVoid = TrsKasirModel.CreateKeluarKas(shift.ShiftId, "KASIR-01", "REG02", "P02", "Pasien B", "ORD02", "Refund Void", 10_000m, new[] { pTunai10 }, OpenTime.AddHours(5));
        trsKeluarVoid.Void("KASIR-01", "Batal refund", OpenTime.AddHours(5));

        _trsKasirRepoMock
            .Setup(x => x.ListByShift(shift.ShiftId))
            .Returns(new[] { trsTerima1, trsTerima2, trsTerimaDebit, trsTerimaVoid, trsKeluar1, trsKeluarTransfer, trsKeluarVoid });

        // SaldoAwal = 100k, Terima = 150k, Keluar = 20k => SaldoSistem = 230k
        var cmd = new CloseKasirShiftCommand(
            ShiftId: shift.ShiftId,
            SaldoAkhirFisik: 230_000m,
            KasirId: "KASIR-01");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.ShiftId.Should().Be(shift.ShiftId);
        response.StatusShift.Should().Be(KasirConst.StatusShiftTutup);
        response.TglJamTutup.Should().Be("2026-11-07 15:00:00");
        response.SaldoAwal.Should().Be(100_000m);
        response.TotalTerimaSistem.Should().Be(150_000m);
        response.TotalKeluarSistem.Should().Be(20_000m);
        response.SaldoAkhirSistem.Should().Be(230_000m);
        response.SaldoAkhirFisik.Should().Be(230_000m);
        response.SelisihKas.Should().Be(0m);

        _kasirShiftRepoMock.Verify(x => x.Save(It.Is<KasirShiftModel>(s =>
            s.ShiftId == shift.ShiftId
            && s.IsClosed
            && s.TotalTerimaSistem == 150_000m
            && s.TotalKeluarSistem == 20_000m
            && s.SaldoAkhirFisik == 230_000m
            && s.SelisihKas == 0m)), Times.Once);
    }

    [Fact]
    public async Task GivenDiscrepancyInSaldoFisik_WhenHandle_ThenCalculatesSelisihKasCorrectly()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 100_000m, OpenTime);
        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);

        var pTunai150 = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 150_000m, 0m);
        var trs = TrsKasirModel.CreateTerimaKas(shift.ShiftId, "KASIR-01", "REG01", "P01", "Pasien A", "ORD01", "Ket", 150_000m, new[] { pTunai150 }, OpenTime);
        _trsKasirRepoMock.Setup(x => x.ListByShift(shift.ShiftId)).Returns(new[] { trs });

        // SaldoSistem = 100k + 150k - 0 = 250k. Fisik = 240k => Selisih -10k
        var cmd = new CloseKasirShiftCommand(shift.ShiftId, 240_000m, "KASIR-01");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.SaldoAkhirSistem.Should().Be(250_000m);
        response.SaldoAkhirFisik.Should().Be(240_000m);
        response.SelisihKas.Should().Be(-10_000m);
    }

    [Fact]
    public async Task GivenShiftNotFound_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _kasirShiftRepoMock.Setup(x => x.GetKey("SH_NOT_FOUND")).Returns((KasirShiftModel?)null);

        var cmd = new CloseKasirShiftCommand("SH_NOT_FOUND", 100_000m, "KASIR-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task GivenShiftAlreadyClosed_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 100_000m, OpenTime);
        shift.CloseShift(100_000m, 0m, 0m, OpenTime.AddHours(8));

        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);

        var cmd = new CloseKasirShiftCommand(shift.ShiftId, 100_000m, "KASIR-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*sudah berstatus TUTUP*");
    }

    [Fact]
    public async Task GivenKasirIdMismatch_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var shift = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 100_000m, OpenTime);
        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);

        var cmd = new CloseKasirShiftCommand(shift.ShiftId, 100_000m, "KASIR-02");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*bukan milik kasir*");
    }

    [Fact]
    public async Task GivenNegativeSaldoAkhirFisik_WhenHandle_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new CloseKasirShiftCommand("SH00000001", -10_000m, "KASIR-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "KASIR-01")]
    [InlineData("   ", "KASIR-01")]
    [InlineData("SH00000001", "")]
    [InlineData("SH00000001", "   ")]
    public async Task GivenEmptyShiftIdOrKasirId_WhenHandle_ThenThrowsArgumentException(string shiftId, string kasirId)
    {
        // Arrange
        var cmd = new CloseKasirShiftCommand(shiftId, 100_000m, kasirId);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Validator_WhenValidCommand_ShouldPassValidation()
    {
        var cmd = new CloseKasirShiftCommand("SH00000001", 100_000m, "KASIR-01");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenEmptyShiftId_ShouldFailValidation()
    {
        var cmd = new CloseKasirShiftCommand("", 100_000m, "KASIR-01");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloseKasirShiftCommand.ShiftId));
    }

    [Fact]
    public void Validator_WhenEmptyKasirId_ShouldFailValidation()
    {
        var cmd = new CloseKasirShiftCommand("SH00000001", 100_000m, "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloseKasirShiftCommand.KasirId));
    }

    [Fact]
    public void Validator_WhenNegativeSaldoAkhirFisik_ShouldFailValidation()
    {
        var cmd = new CloseKasirShiftCommand("SH00000001", -500m, "KASIR-01");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloseKasirShiftCommand.SaldoAkhirFisik));
    }
}
