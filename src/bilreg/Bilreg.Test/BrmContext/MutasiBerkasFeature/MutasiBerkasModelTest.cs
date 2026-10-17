using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasModelTest
{
    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSamplePermintaan(string id = "MB00000001") =>
        MutasiBerkasModel.CreatePermintaan(
            mutasiBerkasId: id,
            trsBuatId: "RG2610170001",
            pasienId: "00123456",
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR01",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.Default);

    private static MutasiBerkasModel CreateSampleDispatched(string id = "MK00000001", decimal qtyRi = 1, decimal qtyRj = 1) =>
        MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: id,
            trsBuatId: "RG2610170001",
            pasienId: "00123456",
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 15, 0),
            petugasId: "USR01",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.CreatePinjam(qtyRi, qtyRj));

    #region DEFAULT & KEY TESTS
    [Fact]
    public void GivenDefault_WhenCreated_ThenHasInitialDefaultValues()
    {
        var sut = MutasiBerkasModel.Default;

        sut.MutasiBerkasId.Should().Be("-");
        sut.TrsBuatId.Should().Be("-");
        sut.PasienId.Should().Be("-");
        sut.Status.Should().Be(MutasiBerkasStatusEnum.Permintaan);
        sut.Volume.TotalPinjam.Should().Be(0);
        sut.CetakTracerInfo.FrekuensiCetak.Should().Be(0);
        sut.VoidInfo.IsVoid.Should().BeFalse();
        sut.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void GivenKeyFactory_WhenCalled_ThenReturnsExpectedKey()
    {
        var key = MutasiBerkasModel.Key("MK00000001");

        key.Should().NotBeNull();
        key.MutasiBerkasId.Should().Be("MK00000001");
    }
    #endregion

    #region FACTORY CREATION TESTS
    [Fact]
    public void GivenCreatePermintaan_WhenValidParameters_ThenInitializesInPermintaanStatus()
    {
        var sut = CreateSamplePermintaan("REQ001");

        sut.MutasiBerkasId.Should().Be("REQ001");
        sut.TrsBuatId.Should().Be("RG2610170001");
        sut.PasienId.Should().Be("00123456");
        sut.PetugasId.Should().Be("USR01");
        sut.Status.Should().Be(MutasiBerkasStatusEnum.Permintaan);
        sut.IsVoid.Should().BeFalse();
        sut.Volume.TotalPinjam.Should().Be(0);
    }

    [Fact]
    public void GivenCreatePermintaan_WhenMissingRequiredFields_ThenThrowsException()
    {
        var actNullId = () => MutasiBerkasModel.CreatePermintaan(null!, "RG01", "MR01", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());
        var actNullTrs = () => MutasiBerkasModel.CreatePermintaan("ID01", "", "MR01", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());
        var actNullMr = () => MutasiBerkasModel.CreatePermintaan("ID01", "RG01", " ", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());
        var actNullPetugas = () => MutasiBerkasModel.CreatePermintaan("ID01", "RG01", "MR01", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), null!, CreateSampleJadwal());
        var actNullJadwal = () => MutasiBerkasModel.CreatePermintaan("ID01", "RG01", "MR01", new DateOnly(2026, 1, 1), new TimeOnly(8, 0), "USR01", null!);

        actNullId.Should().Throw<ArgumentException>();
        actNullTrs.Should().Throw<ArgumentException>();
        actNullMr.Should().Throw<ArgumentException>();
        actNullPetugas.Should().Throw<ArgumentException>();
        actNullJadwal.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenCreateKirim_WhenValidParameters_ThenInitializesInDispatchedStatus()
    {
        var volume = VolumeBerkasType.CreatePinjam(0, 1);
        var sut = MutasiBerkasModel.CreateKirim("MK00000001", "RG01", "00123456", new DateOnly(2026, 10, 17), new TimeOnly(8, 30), "USR01", CreateSampleJadwal(), volume);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Dispatched);
        sut.Volume.QtyPinjamRj.Should().Be(1);
        sut.Volume.QtyPinjamRi.Should().Be(0);
        sut.Volume.TotalPinjam.Should().Be(1);
    }

    [Fact]
    public void GivenCreateKirim_WhenZeroVolume_ThenThrowsArgumentException()
    {
        var act = () => MutasiBerkasModel.CreateKirim("MK00000001", "RG01", "00123456", new DateOnly(2026, 10, 17), new TimeOnly(8, 30), "USR01", CreateSampleJadwal(), VolumeBerkasType.Default);

        act.Should().Throw<ArgumentException>().WithMessage("*lebih dari 0*");
    }
    #endregion

    #region KIRIM (DISPATCH) TESTS
    [Fact]
    public void GivenPermintaan_WhenKirimCalledWithValidVolume_ThenStatusTransitionsToDispatched()
    {
        var sut = CreateSamplePermintaan();
        var volume = VolumeBerkasType.CreatePinjam(1, 1);
        var dispatchDate = new DateOnly(2026, 10, 17);
        var dispatchTime = new TimeOnly(8, 30, 0);

        sut.Kirim("USR_FILING", dispatchDate, dispatchTime, volume);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Dispatched);
        sut.PetugasId.Should().Be("USR_FILING");
        sut.TglMutasi.Should().Be(dispatchDate);
        sut.JamMutasi.Should().Be(dispatchTime);
        sut.Volume.TotalPinjam.Should().Be(2);
        sut.Volume.QtyPinjamRi.Should().Be(1);
        sut.Volume.QtyPinjamRj.Should().Be(1);
    }

    [Fact]
    public void GivenMissingStatus_WhenKirimCalled_ThenTransitionsBackToDispatched()
    {
        var sut = CreateSamplePermintaan();
        sut.TandaiHilang("USR01", new DateOnly(2026, 10, 17), new TimeOnly(8, 10, 0));
        sut.Status.Should().Be(MutasiBerkasStatusEnum.Missing);

        var volume = VolumeBerkasType.CreatePinjam(0, 1);
        sut.Kirim("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0), volume);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Dispatched);
        sut.Volume.TotalPinjam.Should().Be(1);
    }

    [Fact]
    public void GivenAlreadyDispatched_WhenKirimCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSampleDispatched();

        var act = () => sut.Kirim("USR02", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0), VolumeBerkasType.CreatePinjam(1, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*tidak dapat dikirim dari status Dispatched*");
    }

    [Fact]
    public void GivenKirim_WhenZeroVolume_ThenThrowsArgumentException()
    {
        var sut = CreateSamplePermintaan();

        var act = () => sut.Kirim("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0), VolumeBerkasType.Default);

        act.Should().Throw<ArgumentException>().WithMessage("*lebih dari 0*");
    }

    [Fact]
    public void GivenVoidedMutasi_WhenKirimCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();
        sut.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 5, 0), "Salah request");

        var act = () => sut.Kirim("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0), VolumeBerkasType.CreatePinjam(1, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }
    #endregion

    #region TERIMA (RECEIVE) TESTS
    [Fact]
    public void GivenDispatched_WhenTerimaCalled_ThenStatusTransitionsToReceived()
    {
        var sut = CreateSampleDispatched();
        var tglTerima = new DateOnly(2026, 10, 17);
        var jamTerima = new TimeOnly(8, 45, 0);

        sut.Terima("USR_POLI", tglTerima, jamTerima);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Received);
        sut.PetugasId.Should().Be("USR_POLI");
        sut.TglMutasi.Should().Be(tglTerima);
        sut.JamMutasi.Should().Be(jamTerima);
    }

    [Fact]
    public void GivenStatusNotDispatched_WhenTerimaCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();

        var act = () => sut.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*harus Dispatched*");
    }

    [Fact]
    public void GivenVoidedDispatched_WhenTerimaCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSampleDispatched();
        sut.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 20, 0));

        var act = () => sut.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }
    #endregion

    #region KEMBALI (RETURN) TESTS
    [Fact]
    public void GivenReceived_WhenKembaliCalledWithFullVolume_ThenStatusTransitionsToReturnedAndLengkap()
    {
        var sut = CreateSampleDispatched(qtyRi: 1, qtyRj: 1);
        sut.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));

        var tglKembali = new DateOnly(2026, 10, 17);
        var jamKembali = new TimeOnly(12, 0, 0);
        sut.Kembali("USR_FILING", tglKembali, jamKembali, qtyKembaliRi: 1, qtyKembaliRj: 1);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Returned);
        sut.PetugasId.Should().Be("USR_FILING");
        sut.TglMutasi.Should().Be(tglKembali);
        sut.JamMutasi.Should().Be(jamKembali);
        sut.Volume.TotalKembali.Should().Be(2);
        sut.Volume.SisaPinjam.Should().Be(0);
        sut.Volume.IsLengkapKembali.Should().BeTrue();
    }

    [Fact]
    public void GivenDispatchedDirectly_WhenKembaliCalled_ThenStatusTransitionsToReturned()
    {
        var sut = CreateSampleDispatched(qtyRi: 0, qtyRj: 1);

        sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), qtyKembaliRi: 0, qtyKembaliRj: 1);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Returned);
        sut.Volume.IsLengkapKembali.Should().BeTrue();
    }

    [Fact]
    public void GivenReceived_WhenKembaliPartialVolume_ThenStatusReturnedWithRemainingBalance()
    {
        var sut = CreateSampleDispatched(qtyRi: 1, qtyRj: 1);
        sut.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));

        sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), qtyKembaliRi: 0, qtyKembaliRj: 1);

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Returned);
        sut.Volume.QtyKembaliRj.Should().Be(1);
        sut.Volume.QtyKembaliRi.Should().Be(0);
        sut.Volume.SisaPinjam.Should().Be(1);
        sut.Volume.IsLengkapKembali.Should().BeFalse();
    }

    [Fact]
    public void GivenInvalidStatusForKembali_WhenKembaliCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();

        var act = () => sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), 0, 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*harus Dispatched atau Received*");
    }

    [Fact]
    public void GivenKembali_WhenReturnQuantityExceedsBorrowed_ThenThrowsArgumentException()
    {
        var sut = CreateSampleDispatched(qtyRi: 0, qtyRj: 1);
        sut.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));

        var actRi = () => sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), qtyKembaliRi: 1, qtyKembaliRj: 1);
        var actRj = () => sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), qtyKembaliRi: 0, qtyKembaliRj: 2);

        actRi.Should().Throw<ArgumentException>().WithMessage("*rawat inap kembali melebihi jumlah pinjam*");
        actRj.Should().Throw<ArgumentException>().WithMessage("*rawat jalan kembali melebihi jumlah pinjam*");
    }

    [Fact]
    public void GivenKembali_WhenNegativeOrZeroQuantity_ThenThrowsArgumentException()
    {
        var sut = CreateSampleDispatched(qtyRi: 1, qtyRj: 1);

        var actZero = () => sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), 0, 0);
        var actNeg = () => sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), -1, 1);

        actZero.Should().Throw<ArgumentException>().WithMessage("*harus lebih dari 0*");
        actNeg.Should().Throw<ArgumentException>().WithMessage("*tidak boleh negatif*");
    }
    #endregion

    #region TANDAI HILANG (MISSING) TESTS
    [Theory]
    [InlineData(MutasiBerkasStatusEnum.Permintaan)]
    [InlineData(MutasiBerkasStatusEnum.Dispatched)]
    [InlineData(MutasiBerkasStatusEnum.Received)]
    public void GivenActiveStatus_WhenTandaiHilangCalled_ThenStatusTransitionsToMissing(MutasiBerkasStatusEnum initialStatus)
    {
        var sut = CreateSamplePermintaan();
        if (initialStatus == MutasiBerkasStatusEnum.Dispatched)
        {
            sut.Kirim("USR01", new DateOnly(2026, 10, 17), new TimeOnly(8, 30), VolumeBerkasType.CreatePinjam(0, 1));
        }
        else if (initialStatus == MutasiBerkasStatusEnum.Received)
        {
            sut.Kirim("USR01", new DateOnly(2026, 10, 17), new TimeOnly(8, 30), VolumeBerkasType.CreatePinjam(0, 1));
            sut.Terima("USR02", new DateOnly(2026, 10, 17), new TimeOnly(8, 45));
        }

        sut.TandaiHilang("USR_PETUGAS", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0));

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Missing);
        sut.PetugasId.Should().Be("USR_PETUGAS");
    }

    [Fact]
    public void GivenReturnedStatus_WhenTandaiHilangCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSampleDispatched(qtyRi: 0, qtyRj: 1);
        sut.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), 0, 1);

        var act = () => sut.TandaiHilang("USR01", new DateOnly(2026, 10, 17), new TimeOnly(12, 5, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dikembalikan*");
    }

    [Fact]
    public void GivenAlreadyMissing_WhenTandaiHilangCalledAgain_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();
        sut.TandaiHilang("USR01", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0));

        var act = () => sut.TandaiHilang("USR02", new DateOnly(2026, 10, 17), new TimeOnly(9, 30, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah berstatus hilang*");
    }
    #endregion

    #region VOID TESTS
    [Fact]
    public void GivenActiveMutasi_WhenVoidCalled_ThenVoidInfoRecordedAndStatusVoidedNonDestructively()
    {
        var sut = CreateSampleDispatched(qtyRi: 1, qtyRj: 1);
        var tglVoid = new DateOnly(2026, 10, 17);
        var jamVoid = new TimeOnly(10, 0, 0);

        sut.Void("USR_SUPERVISOR", tglVoid, jamVoid, "Pendaftaran dibatalkan pasien");

        sut.Status.Should().Be(MutasiBerkasStatusEnum.Void);
        sut.IsVoid.Should().BeTrue();
        sut.VoidInfo.IsVoid.Should().BeTrue();
        sut.VoidInfo.PetugasVoid.Should().Be("USR_SUPERVISOR");
        sut.VoidInfo.TglVoid.Should().Be(tglVoid);
        sut.VoidInfo.JamVoid.Should().Be(jamVoid);
        sut.VoidInfo.AlasanVoid.Should().Be("Pendaftaran dibatalkan pasien");

        // Non-destructive: previous context remains preserved
        sut.PasienId.Should().Be("00123456");
        sut.TrsBuatId.Should().Be("RG2610170001");
        sut.Volume.TotalPinjam.Should().Be(2);
    }

    [Fact]
    public void GivenAlreadyVoided_WhenVoidCalledAgain_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();
        sut.Void("USR_SUPERVISOR", new DateOnly(2026, 10, 17), new TimeOnly(10, 0, 0));

        var act = () => sut.Void("USR_ANOTHER", new DateOnly(2026, 10, 17), new TimeOnly(10, 5, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }
    #endregion

    #region CETAK TRACER TESTS
    [Fact]
    public void GivenMutasi_WhenCetakTracerCalled_ThenIncrementsFrequencyAndRecordsAudit()
    {
        var sut = CreateSampleDispatched();
        sut.CetakTracerInfo.FrekuensiCetak.Should().Be(0);
        sut.CetakTracerInfo.IsPrinted.Should().BeFalse();

        var tglCetak = new DateOnly(2026, 10, 17);
        var jamCetak = new TimeOnly(8, 20, 0);
        sut.CetakTracer("USR_FILING", tglCetak, jamCetak);

        sut.CetakTracerInfo.FrekuensiCetak.Should().Be(1);
        sut.CetakTracerInfo.IsPrinted.Should().BeTrue();
        sut.CetakTracerInfo.PetugasCetak.Should().Be("USR_FILING");
        sut.CetakTracerInfo.TglCetak.Should().Be(tglCetak);
        sut.CetakTracerInfo.JamCetak.Should().Be(jamCetak);

        // Print again
        var jamCetak2 = new TimeOnly(8, 25, 0);
        sut.CetakTracer("USR_FILING2", tglCetak, jamCetak2);

        sut.CetakTracerInfo.FrekuensiCetak.Should().Be(2);
        sut.CetakTracerInfo.PetugasCetak.Should().Be("USR_FILING2");
        sut.CetakTracerInfo.JamCetak.Should().Be(jamCetak2);
    }

    [Fact]
    public void GivenVoidedMutasi_WhenCetakTracerCalled_ThenThrowsInvalidOperationException()
    {
        var sut = CreateSamplePermintaan();
        sut.Void("USR_SUPERVISOR", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0));

        var act = () => sut.CetakTracer("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(9, 1, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }
    #endregion

    #region VALUE OBJECT INVARIANTS TESTS
    [Fact]
    public void GivenVolumeBerkasType_WhenCalculated_ThenMatchesTotals()
    {
        var vol = new VolumeBerkasType(1m, 2m, 1m, 1m);

        vol.TotalPinjam.Should().Be(3);
        vol.TotalKembali.Should().Be(2);
        vol.SisaPinjam.Should().Be(1);
        vol.IsLengkapKembali.Should().BeFalse();

        var returnedVol = vol.CatatKembali(1m, 2m);
        returnedVol.TotalKembali.Should().Be(3);
        returnedVol.SisaPinjam.Should().Be(0);
        returnedVol.IsLengkapKembali.Should().BeTrue();
    }

    [Fact]
    public void GivenVolumeBerkasType_WhenNegativeOrExcessiveReturn_ThenThrowsException()
    {
        var actNegative = () => new VolumeBerkasType(-1m, 0m, 0m, 0m);
        var actExcessRi = () => new VolumeBerkasType(1m, 1m, 2m, 1m);
        var actExcessRj = () => new VolumeBerkasType(1m, 1m, 1m, 2m);

        actNegative.Should().Throw<ArgumentException>().WithMessage("*tidak boleh negatif*");
        actExcessRi.Should().Throw<ArgumentException>().WithMessage("*melebihi*");
        actExcessRj.Should().Throw<ArgumentException>().WithMessage("*melebihi*");
    }

    [Fact]
    public void GivenJadwalPelayananType_WhenCreatedWithValidData_ThenPropertiesAssigned()
    {
        var jadwal = JadwalPelayananType.Create("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

        jadwal.LayananId.Should().Be("POL01");
        jadwal.DokterId.Should().Be("DR001");
        jadwal.TglPraktek.Should().Be(new DateOnly(2026, 10, 17));
        jadwal.JamPraktek.Should().Be("08:00");
        jadwal.SesiPoliId.Should().Be("1");
    }

    [Fact]
    public void GivenJadwalPelayananType_WhenMissingFields_ThenThrowsException()
    {
        var act = () => JadwalPelayananType.Create("", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");
        act.Should().Throw<ArgumentException>();
    }
    #endregion
}
