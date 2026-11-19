using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class CreateTerimaKasCommandTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly Mock<IKasirStatusSyncDal> _statusSyncDalMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly Mock<ILoketKasirDal> _loketKasirDalMock;
    private readonly Mock<ITrsKasirItemDal> _trsKasirItemDalMock;
    private readonly Mock<ITrsKasirLegacyDal> _trsKasirLegacyDalMock;
    private readonly CreateTerimaKasHandler _sut;
    private readonly CreateTerimaKasValidator _validator;

    private static readonly DateTime FixedNow = new(2026, 11, 7, 10, 0, 0);

    public CreateTerimaKasCommandTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _statusSyncDalMock = new Mock<IKasirStatusSyncDal>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(FixedNow);
        _loketKasirDalMock = new Mock<ILoketKasirDal>();
        _trsKasirItemDalMock = new Mock<ITrsKasirItemDal>();
        _trsKasirLegacyDalMock = new Mock<ITrsKasirLegacyDal>();

        _sut = new CreateTerimaKasHandler(
            _kasirShiftRepoMock.Object,
            _trsKasirRepoMock.Object,
            _statusSyncDalMock.Object,
            _tglJamProviderMock.Object,
            _loketKasirDalMock.Object,
            _trsKasirItemDalMock.Object,
            _trsKasirLegacyDalMock.Object);
        _validator = new CreateTerimaKasValidator();
    }

    private KasirShiftModel CreateOpenShift(string shiftId = "SH26000001", string kasirId = "KASIR-01", string loket = "Loket 01")
    {
        return KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: kasirId,
            loket: loket,
            saldoAwal: 100_000m,
            openTime: FixedNow.AddHours(-2));
    }

    [Fact]
    public async Task GivenValidSinglePayment_WhenShiftActive_ThenCreatesTerimaKasSavesToRepoSyncsAndReturnsResponse()
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

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260001",
            PasienId: "RM001",
            PasienName: "Budi Santoso",
            OrderRef: "BL260001",
            Keterangan: "Pembayaran Poli Penyakit Dalam",
            TotalNilai: 150_000m,
            Payments: new[]
            {
                new PaymentItemInput(KasirConst.MetodeBayarTunai, 200_000m, 50_000m)
            });

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TrsKasirId.Should().StartWith("DX");
        response.JenisTrs.Should().Be(KasirConst.JenisTrsTerima);
        response.TotalNilai.Should().Be(150_000m);
        response.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        response.Payments.Should().HaveCount(1);

        savedModel.Should().NotBeNull();
        savedModel!.TrsKasirId.Should().Be(response.TrsKasirId);
        savedModel.TotalNilai.Should().Be(150_000m);

        _statusSyncDalMock.Verify(x => x.SyncTerima(
            "BL260001",
            "KASIR-01",
            "2026-11-07",
            "10:00:00",
            response.TrsKasirId,
            150_000m,
            "RG260001"), Times.Once);

        _trsKasirLegacyDalMock.Verify(x => x.Insert(
            It.Is<TrsKasirLegacyHdrDto>(h => h.TrsKasirId == response.TrsKasirId && h.SumAll == 150_000m),
            It.IsAny<IEnumerable<TrsKasirLegacyDtlDto>>()), Times.Once);
    }

    [Fact]
    public async Task GivenValidMultiPayment_WhenExactMatch_ThenSucceeds()
    {
        // Arrange
        var shift = CreateOpenShift();
        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260001",
            PasienId: "RM001",
            PasienName: "Budi Santoso",
            OrderRef: "DP260001",
            Keterangan: "Setoran Deposit",
            TotalNilai: 500_000m,
            Payments: new[]
            {
                new PaymentItemInput(KasirConst.MetodeBayarTunai, 200_000m, 0m),
                new PaymentItemInput(KasirConst.MetodeBayarDebit, 300_000m, 0m, "12345678", "APPR888", "BCA")
            });

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TotalNilai.Should().Be(500_000m);
        response.Payments.Should().HaveCount(2);

        _trsKasirRepoMock.Verify(x => x.Save(It.IsAny<TrsKasirModel>()), Times.Once);
        _statusSyncDalMock.Verify(x => x.SyncTerima(
            "DP260001", "KASIR-01", "2026-11-07", "10:00:00", response.TrsKasirId, 500_000m, "RG260001"), Times.Once);
    }

    [Fact]
    public async Task GivenTunaiPaymentWithoutCoaId_WhenHandled_AbsorbsCoaIdFromLoket()
    {
        // Arrange
        var shift = CreateOpenShift(loket: "LOK-POLI");
        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);
        _loketKasirDalMock.Setup(x => x.GetData("LOK-POLI"))
            .Returns(new LoketKasirDto("LOK-POLI", "Loket Poliklinik", "1110203", "KAS POLIKLINIK", true, "Loket Poliklinik"));

        TrsKasirModel? savedModel = null;
        _trsKasirRepoMock.Setup(x => x.Save(It.IsAny<TrsKasirModel>()))
            .Callback<TrsKasirModel>(m => savedModel = m);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260001",
            PasienId: "RM001",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "Poli",
            TotalNilai: 100_000m,
            Payments: new[]
            {
                new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m, 0m, CoaId: "")
            });

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        savedModel.Should().NotBeNull();
        savedModel!.Payments[0].CoaId.Should().Be("1110203");
        response.Payments.First().CoaId.Should().Be("1110203");
        _trsKasirLegacyDalMock.Verify(x => x.Insert(
            It.Is<TrsKasirLegacyHdrDto>(h => h.KdRekKas == "1110203"),
            It.IsAny<IEnumerable<TrsKasirLegacyDtlDto>>()), Times.Once);
    }

    [Fact]
    public async Task GivenPaymentWithItems_WhenHandled_PersistsItemsToBILRG_TrsKasirItemAndDualWritesLegacy()
    {
        // Arrange
        var shift = CreateOpenShift();
        _kasirShiftRepoMock.Setup(x => x.GetKey(shift.ShiftId)).Returns(shift);

        List<TrsKasirItemDto>? insertedItems = null;
        _trsKasirItemDalMock.Setup(x => x.Insert(It.IsAny<IEnumerable<TrsKasirItemDto>>()))
            .Callback<IEnumerable<TrsKasirItemDto>>(items => insertedItems = items.ToList());

        List<TrsKasirLegacyDtlDto>? insertedLegacyDtls = null;
        _trsKasirLegacyDalMock.Setup(x => x.Insert(It.IsAny<TrsKasirLegacyHdrDto>(), It.IsAny<IEnumerable<TrsKasirLegacyDtlDto>>()))
            .Callback<TrsKasirLegacyHdrDto, IEnumerable<TrsKasirLegacyDtlDto>>((h, dtls) => insertedLegacyDtls = dtls.ToList());

        var items = new List<KasirItemBayarInput>
        {
            new("BL01", "RG260001", "2RJ01", "Konsultasi Dokter", 100_000m, 100_000m),
            new("BL02", "RG260001", "4OB01", "Obat Resep", 50_000m, 50_000m)
        };

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG260001",
            PasienId: "RM001",
            PasienName: "Budi",
            OrderRef: "RG260001",
            Keterangan: "Pembayaran Layanan",
            TotalNilai: 150_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 150_000m) },
            Items: items);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Items.Should().NotBeNull();
        response.Items.Should().HaveCount(2);

        insertedItems.Should().NotBeNull();
        insertedItems!.Should().HaveCount(2);
        insertedItems![0].BillId.Should().Be("BL01");
        insertedItems[0].NilaiBayar.Should().Be(100_000m);
        insertedItems[1].BillId.Should().Be("BL02");
        insertedItems[1].NilaiBayar.Should().Be(50_000m);

        insertedLegacyDtls.Should().NotBeNull();
        insertedLegacyDtls!.Should().HaveCount(2);
        insertedLegacyDtls![0].TrsBillId.Should().Be("BL01");
        insertedLegacyDtls[0].IsJasa.Should().BeTrue();
        insertedLegacyDtls[1].TrsBillId.Should().Be("BL02");
        insertedLegacyDtls[1].IsObat.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNonExistentShift_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _kasirShiftRepoMock
            .Setup(x => x.GetKey("SH99999999"))
            .Returns((KasirShiftModel?)null);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: "SH99999999",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m) });

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
        shift.CloseShift(100_000m, 0m, 0m, FixedNow.AddHours(-1));

        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m) });

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
        var shift = CreateOpenShift(kasirId: "KASIR-02");
        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m) });

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*bukan milik kasir*");
    }

    [Fact]
    public async Task GivenMismatchedPaymentTotal_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var shift = CreateOpenShift();
        _kasirShiftRepoMock
            .Setup(x => x.GetKey(shift.ShiftId))
            .Returns(shift);

        var cmd = new CreateTerimaKasCommand(
            ShiftId: shift.ShiftId,
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 80_000m) }); // 80k != 100k

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*tidak sesuai*");
    }

    [Fact]
    public void GivenValidCommand_WhenValidate_ThenPasses()
    {
        var cmd = new CreateTerimaKasCommand(
            ShiftId: "SH01",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m) });

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "KASIR-01", 1000)]
    [InlineData("SH01", "", 1000)]
    [InlineData("SH01", "KASIR-01", 0)]
    [InlineData("SH01", "KASIR-01", -500)]
    public void GivenInvalidHeader_WhenValidate_ThenFails(string shiftId, string kasirId, decimal totalNilai)
    {
        var cmd = new CreateTerimaKasCommand(
            ShiftId: shiftId,
            KasirId: kasirId,
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: totalNilai,
            Payments: new[] { new PaymentItemInput(KasirConst.MetodeBayarTunai, 100_000m) });

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GivenEmptyPayments_WhenValidate_ThenFails()
    {
        var cmd = new CreateTerimaKasCommand(
            ShiftId: "SH01",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: Array.Empty<PaymentItemInput>());

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Payments");
    }

    [Fact]
    public void GivenInvalidMetodeBayar_WhenValidate_ThenFails()
    {
        var cmd = new CreateTerimaKasCommand(
            ShiftId: "SH01",
            KasirId: "KASIR-01",
            RegId: "RG01",
            PasienId: "RM01",
            PasienName: "Budi",
            OrderRef: "BL01",
            Keterangan: "",
            TotalNilai: 100_000m,
            Payments: new[] { new PaymentItemInput("CRYPTO", 100_000m) });

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Payments[0].MetodeBayar"));
    }
}
