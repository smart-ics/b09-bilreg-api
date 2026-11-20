using System.Data;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class BilrgTataRekeningDalTest
{
    private readonly BilrgTataRekeningDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly RegAktifDal _regAktifDal = new(ConnStringHelper.GetTestEnv());

    private static BilrgTataRekeningDto FakerHeader(string regId = "REG-TRK-TEST-01") =>
        new(
            RegId: regId,
            Status: 0,
            PetugasVerif: "PETUGAS01",
            FinalizationDate: new DateTime(3000, 1, 1),
            FinVerifStatus: 0,
            FinVerifPetugas: "",
            FinVerifDate: new DateTime(3000, 1, 1),
            IsAllocated: false,
            SettlementInitiated: false,
            Version: 1);

    private static RegAktifDto FakerRegAktif(string regId = "REG-TRK-TEST-01", string jenisReg = "0") =>
        new(
            RegId: regId,
            RegDate: new DateTime(2026, 11, 18, 10, 0, 0),
            PasienId: "MR-TEST-01",
            JenisReg: jenisReg,
            LayananId: "LYN-01",
            DokterId: "DR-01",
            TipeJaminanId: "JAM-01",
            PasienName: "TEST PASIEN",
            TglLahir: "1990-05-15",
            Gender: "L",
            LayananName: "Poli Umum",
            DokterName: "dr. Dokter",
            TipeJaminanName: "Umum",
            StatusTataRekening: 0);

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerHeader());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerHeader();
        _sut.Insert(header);
        _sut.Update(header with { PetugasVerif = "UPDATED", Status = 1 });
        var actual = _sut.GetData(RegModel.Key(header.RegId));
        actual.PetugasVerif.Should().Be("UPDATED");
        actual.Status.Should().Be(1);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerHeader();
        _sut.Insert(header);
        _sut.Delete(RegModel.Key(header.RegId));
        var actual = _sut.GetData(RegModel.Key(header.RegId));
        actual.Should().BeNull();
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerHeader();
        _sut.Insert(header);
        var actual = _sut.GetData(RegModel.Key(header.RegId));
        actual.Should().NotBeNull();
        actual.RegId.Should().Be(header.RegId);
        actual.Status.Should().Be(header.Status);
    }

    [Fact]
    public void UpdateConditionalTest()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerHeader();
        _sut.Insert(header);

        var rowsAffected = _sut.UpdateConditional(header with { Status = 2 }, expectedVersion: 1);
        rowsAffected.Should().Be(1);

        var staleRowsAffected = _sut.UpdateConditional(header with { Status = 3 }, expectedVersion: 1);
        staleRowsAffected.Should().Be(0);
    }

    [Fact]
    public void ListDataQueue_ShouldReturnInnerJoinedDataAndExcludeZeroBill()
    {
        using var trans = TransHelper.NewScope();

        // 1. Registrasi dengan billing (TataRekening + RegAktif) -> Harus muncul di antrean
        const string regWithBill = "RQTEST01";
        _regAktifDal.Insert(FakerRegAktif(regWithBill, jenisReg: "0"));
        _sut.Insert(FakerHeader(regWithBill));

        // 2. Registrasi aktif admisi TANPA billing (Hanya RegAktif) -> Tidak boleh muncul
        const string regZeroBill = "RQTEST02";
        _regAktifDal.Insert(FakerRegAktif(regZeroBill, jenisReg: "0"));

        var queue = _sut.ListDataQueue().ToList();

        queue.Should().Contain(x => x.RegId == regWithBill);
        queue.Should().NotContain(x => x.RegId == regZeroBill);

        var item = queue.First(x => x.RegId == regWithBill);
        item.PasienId.Should().Be("MR-TEST-01");
        item.JenisReg.Should().Be("0");
        item.LayananId.Should().Be("LYN-01");
        item.DokterId.Should().Be("DR-01");
        item.TipeJaminanId.Should().Be("JAM-01");
        item.StatusTataRekening.Should().Be(0);
    }

    [Fact]
    public void ListDataQueue_WithJenisRegFilter_ShouldFilterProperly()
    {
        using var trans = TransHelper.NewScope();

        const string regRajal = "RQRJL01";
        const string regRanap = "RQRNP01";

        _regAktifDal.Insert(FakerRegAktif(regRajal, jenisReg: "0"));
        _sut.Insert(FakerHeader(regRajal));

        _regAktifDal.Insert(FakerRegAktif(regRanap, jenisReg: "1"));
        _sut.Insert(FakerHeader(regRanap));

        var rajalList = _sut.ListDataQueue(jenisReg: 0).ToList();
        rajalList.Should().Contain(x => x.RegId == regRajal);
        rajalList.Should().NotContain(x => x.RegId == regRanap);

        var ranapList = _sut.ListDataQueue(jenisReg: 1).ToList();
        ranapList.Should().Contain(x => x.RegId == regRanap);
        ranapList.Should().NotContain(x => x.RegId == regRajal);
    }
}
