using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class CreateKeluarKasCommandTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly Mock<IKasirStatusSyncDal> _statusSyncDalMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly Mock<ILoketKasirDal> _loketKasirDalMock;
    private readonly Mock<ITrsKasirLegacyDal> _trsKasirLegacyDalMock;
    private readonly CreateKeluarKasHandler _sut;
    private readonly CreateKeluarKasValidator _validator;

    private static readonly DateTime FixedNow = new(2026, 11, 7, 11, 0, 0);

    public CreateKeluarKasCommandTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _statusSyncDalMock = new Mock<IKasirStatusSyncDal>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(FixedNow);
        _loketKasirDalMock = new Mock<ILoketKasirDal>();
        _trsKasirLegacyDalMock = new Mock<ITrsKasirLegacyDal>();

        _sut = new CreateKeluarKasHandler(
            _kasirShiftRepoMock.Object,
            _trsKasirRepoMock.Object,
            _statusSyncDalMock.Object,
            _tglJamProviderMock.Object,
            _loketKasirDalMock.Object,
            _trsKasirLegacyDalMock.Object);
        _validator = new CreateKeluarKasValidator();
    }

    private KasirShiftModel CreateOpenShift(string shiftId = "SH26000001", string kasirId = "KASIR-01", string loket = "Loket 01")
    {
        return KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: kasirId,
            loket: loket,
            saldoAwal: 500_000m,
            openTime: FixedNow.AddHours(-2));
    }

    [Fact]
    public async Task GivenValidRequest_WhenShiftActive_ThenCreatesKeluarKasSavesToRepoSyncsAndReturnsResponse()
    {
        // Arrange
        var shift = CreateOpenShift();
        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        TrsKasirModel? savedModel = null;
        _trsKasirRepoMock
            .Setup(x => x.Save(It.IsAny<TrsKasirModel>()))
            .Callback<TrsKasirModel>(m => savedModel = m);

        var cmd = new CreateKeluarKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260002",
            PasienId: "RM002",
            PasienName: "Siti Rahma",
            OrderRef: "BL260002",
            Keterangan: "Pengembalian kelebihan bayar rawat inap",
            TotalNilai: 75_000m,
            Payments: new[]
            {
                new PaymentItemInput(KasirConst.MetodeBayarTunai, 75_000m, 0m)
            });

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TrsKasirId.Should().StartWith("DX");
        response.JenisTrs.Should().Be(KasirConst.JenisTrsKeluar);
        response.TotalNilai.Should().Be(75_000m);
        response.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        response.Payments.Should().HaveCount(1);

        savedModel.Should().NotBeNull();
        savedModel!.TrsKasirId.Should().Be(response.TrsKasirId);
        savedModel.JenisTrs.Should().Be(KasirConst.JenisTrsKeluar);

        _statusSyncDalMock.Verify(x => x.SyncKeluar(
            "BL260002",
            "KASIR-01",
            "2026-11-07",
            "11:00:00",
            response.TrsKasirId,
            75_000m,
            "RG260002"), Times.Once);

        _trsKasirLegacyDalMock.Verify(x => x.Insert(
            It.Is<TrsKasirLegacyHdrDto>(h => h.TrsKasirId == response.TrsKasirId && h.SumAll == 75_000m),
            It.IsAny<IEnumerable<TrsKasirLegacyDtlDto>>()), Times.Once);
    }

    [Fact]
    public async Task GivenTunaiKeluarWithoutCoaId_WhenHandled_AbsorbsCoaIdFromLoket()
    {
        // Arrange
        var shift = CreateOpenShift(loket: "LOK-IGD");
        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);
        _loketKasirDalMock.Setup(x => x.GetData("LOK-IGD"))
            .Returns(new LoketKasirDto("LOK-IGD", "Loket IGD", "1110202", "KAS IGD", true, "Loket IGD"));

        TrsKasirModel? savedModel = null;
        _trsKasirRepoMock.Setup(x => x.Save(It.IsAny<TrsKasirModel>()))
            .Callback<TrsKasirModel>(m => savedModel = m);

        var cmd = new CreateKeluarKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260002",
            PasienId: "RM002",
            PasienName: "Siti",
            OrderRef: "REF01",
            Keterangan: "Refund",
            TotalNilai: 50_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m, 0m, CoaId: "") });

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        savedModel.Should().NotBeNull();
        savedModel!.Payments[0].CoaId.Should().Be("1110202");
        response.Payments.First().CoaId.Should().Be("1110202");
        _trsKasirLegacyDalMock.Verify(x => x.Insert(
            It.Is<TrsKasirLegacyHdrDto>(h => h.KdRekKas == "1110202"),
            It.IsAny<IEnumerable<TrsKasirLegacyDtlDto>>()), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentShift_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _kasirShiftRepoMock
            .Setup(x => x.GetKey("SH99999999"))
            .Returns((KasirShiftModel?)null);

        var cmd = new CreateKeluarKasCommand(
            ShiftId: "SH99999999",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 50_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m) });

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task GivenClosedShift_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var shift = CreateOpenShift();
        shift.CloseShift(500_000m, 0m, 0m, FixedNow.AddHours(-1));

        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateKeluarKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 50_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m) });

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*TUTUP*");
    }

    [Fact]
    public async Task GivenShiftBelongingToAnotherKasir_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var shift = CreateOpenShift(kasirId: "KASIR-OTHER");
        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateKeluarKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 50_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m) });

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*bukan milik kasir*");
    }

    [Fact]
    public void GivenValidCommand_WhenValidate_ThenPasses()
    {
        var cmd = new CreateKeluarKasCommand(
            ShiftId: "SH01",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 50_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m) });

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "KASIR-01", 50000)]
    [InlineData("SH01", "", 50000)]
    [InlineData("SH01", "KASIR-01", 0)]
    [InlineData("SH01", "KASIR-01", -100)]
    public void GivenInvalidInputs_WhenValidate_ThenFails(string shiftId, string kasirId, decimal totalNilai)
    {
        var cmd = new CreateKeluarKasCommand(
            ShiftId: shiftId,
            KasirId: kasirId,
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: totalNilai,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 50_000m) });

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
