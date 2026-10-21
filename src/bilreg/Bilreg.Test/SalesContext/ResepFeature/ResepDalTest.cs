using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Infrastructure.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.SalesContext.ResepFeature;

[Collection("ResepDatabaseTests")]
public class ResepDalTest
{
    private readonly ResepDal _sut;
    private readonly ResepBrgDal _brgDal;
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public ResepDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new ResepDal(_dbOptions);
        _brgDal = new ResepBrgDal(_dbOptions);
    }

    private static string NewTestResepId() => $"KPT{Random.Shared.Next(1000000, 9999999)}";

    private static ResepDto FakerData(string resepId) => new()
    {
        ResepId = resepId,
        TglJam = new DateTime(2026, 10, 12, 10, 30, 0),
        UserId = "TESTUSR",
        RegId = "RG00213695",
        PasienId = "337502200016068",
        PasienName = "TANAAH",
        TinggiBadan = 165,
        BeratBadan = 60,
        Lpb = 75,
        LayananId = "2RJ01",
        LayananName = "Poli Umum",
        LayananStokId = "4FM01",
        LayananStokName = "Farmasi Rawat Jalan",
        DokterId = "DR01",
        DokterName = "Dr Spesialis",
        UrgenitasId = "U",
        UrgenitasName = "CITO",
        Iter = 1,
        Description = "R/ Amoxicillin 500mg",
        TipeBarangId = "01",
        TipeBarangName = "Obat",
        TglRencanaPeriksa = new DateTime(2026, 10, 15),
        ResepAsalReffId = "KP00000001",
        VoidReason = "-",
        TglVoid = "3000-01-01",
        JamVoid = "00:00:00",
        UserVoidId = string.Empty
    };

    [Fact]
    public void Insert_And_GetData_ReturnsInsertedRecordWithAllColumns()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);

        _sut.Insert(dto);
        var actual = _sut.GetData(ResepModel.Key(resepId));

        actual.Should().NotBeNull();
        actual.ResepId.Should().Be(dto.ResepId);
        actual.LayananId.Should().Be(dto.LayananId);
        actual.LayananStokId.Should().Be(dto.LayananStokId);
        actual.TglRencanaPeriksa.Date.Should().Be(dto.TglRencanaPeriksa.Date);
        actual.ResepAsalReffId.Should().Be(dto.ResepAsalReffId);
        actual.Description.Should().Be(dto.Description);
        actual.Iter.Should().Be(dto.Iter);
        actual.TinggiBadan.Should().Be(dto.TinggiBadan);
        actual.BeratBadan.Should().Be(dto.BeratBadan);
        actual.Lpb.Should().Be(dto.Lpb);
    }

    [Fact]
    public void Update_UpdatesAllColumnsIncludingVoidAndLayananStok()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        dto.LayananStokId = "4FM02";
        dto.TglRencanaPeriksa = new DateTime(2026, 10, 20);
        dto.ResepAsalReffId = "KP00000002";
        dto.Description = "[BATAL: Salah dosis] R/ Amoxicillin 500mg";
        dto.TglVoid = "2026-10-12";
        dto.JamVoid = "11:00:00";
        dto.UserVoidId = "VOIDUSR";

        _sut.Update(dto);
        var actual = _sut.GetData(ResepModel.Key(resepId));

        actual.Should().NotBeNull();
        actual.LayananStokId.Should().Be("4FM02");
        actual.TglRencanaPeriksa.Date.Should().Be(new DateTime(2026, 10, 20));
        actual.ResepAsalReffId.Should().Be("KP00000002");
        actual.Description.Should().Be("[BATAL: Salah dosis] R/ Amoxicillin 500mg");
        actual.TglVoid.Should().Be("2026-10-12");
        actual.JamVoid.Should().Be("11:00:00");
        actual.UserVoidId.Should().Be("VOIDUSR");
    }

    [Fact]
    public void Delete_RemovesRecordAndNote()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        _sut.Delete(ResepModel.Key(resepId));
        var actual = _sut.GetData(ResepModel.Key(resepId));

        actual.Should().BeNull();
    }

    [Fact]
    public void ListData_ReturnsPrescriptionsForRegId()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        var list = _sut.ListData(RegModel.Key("RG00213695"));

        list.Should().Contain(x => x.ResepId == resepId);
    }

    [Fact]
    public void HasBeenProcessed_ReturnsFalseForNewResep()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        var processed = _sut.HasBeenProcessed(resepId);

        processed.Should().BeFalse();
    }

    [Fact]
    public void HasBeenProcessed_ReturnsTrueWhenStatusDobillGreaterThanZero()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        using (var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value)))
        {
            conn.Execute("UPDATE ta_trs_kartu_periksa SET fn_status_dobill = 1 WHERE fs_kd_trs = @ResepId", new { ResepId = resepId });
        }

        var processed = _sut.HasBeenProcessed(resepId);

        processed.Should().BeTrue();
    }

    [Fact]
    public void LockPrescription_SetsStatusDobillToOne()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var dto = FakerData(resepId);
        _sut.Insert(dto);

        _sut.LockPrescription(resepId);

        _sut.HasBeenProcessed(resepId).Should().BeTrue();

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        var status = conn.ExecuteScalar<int>("SELECT fn_status_dobill FROM ta_trs_kartu_periksa WHERE fs_kd_trs = @ResepId", new { ResepId = resepId });
        status.Should().Be(1);
    }

    [Fact]
    public void FindPotentialDuplicate_ReturnsDuplicateWhenSamePatientAndDay()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var date = new DateTime(2026, 10, 12);
        var dto = FakerData(resepId);
        dto.TglJam = new DateTime(2026, 10, 12, 8, 0, 0);
        _sut.Insert(dto);

        var testBrgId = "TESTBRG01";
        _brgDal.Insert([
            new ResepBrgDto(
                resepId, 1, testBrgId, "Amoxicillin Test", 10, "TAB", 0,
                false, false, string.Empty, 0, string.Empty,
                "3x1", "Sesudah makan", 3, 1, "Tablet", "ORAL", "Sesudah makan", "08:00, 14:00, 20:00")
        ]);

        var duplicates = _sut.FindPotentialDuplicate("337502200016068", date, [testBrgId]).ToList();
        duplicates.Should().Contain(x => x.ResepId == resepId);

        var nonDuplicates = _sut.FindPotentialDuplicate("337502200016068", date, ["NON_EXISTENT_MED"]);
        nonDuplicates.Should().BeEmpty();
    }
}
