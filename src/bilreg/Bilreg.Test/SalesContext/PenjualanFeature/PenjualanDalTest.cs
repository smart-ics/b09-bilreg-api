using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanDalTest
{
    private readonly PenjualanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PenjualanDto FakerData() => new()
    {
        PenjualanId = "DUTST00001",
        TglJam = new DateTime(2026, 8, 6, 10, 0, 0),
        UserId = "U1",
        ResepId = "KPTEST0001",
        RegId = "RG00000001",
        PasienId = "MR0001",
        PasienName = "Pasien Tes",
        LayananId = "LYJ01",
        LayananName = "Apotek RJ",
        LayananResepId = "LYR01",
        LayananResepName = "Poli Dalam",
        DokterId = "DR00000001",
        DokterName = "Dr Tes",
        TipeJaminanId = "00000",
        TipeJaminanName = "Umum",
        TipeBarangId = "01",
        TipeBarangName = "Obat",
        SumSubTotal = 10000,
        SumBiaya = 200,
        SumTax = 0,
        SubTotal = 10200,
        DiskonLain = 0,
        BiayaLain = 0,
        GrandTotal = 10200,
        Pembulatan = 0,
        Bulat = 0,
        TglVoid = "3000-01-01",
        JamVoid = "00:00:00",
        UserVoidId = "-"
    };

    private static IPenjualanKey FakerKey() => PenjualanModel.Key("DUTST00001");

    [Fact(Skip = "Requires tb_trs_dobill_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
    }

    [Fact(Skip = "Requires tb_trs_dobill_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var data = FakerData();
        _sut.Insert(data);

        data.GrandTotal = 9900;
        data.DiskonLain = 300;
        _sut.Update(data);
    }

    [Fact(Skip = "Requires tb_trs_dobill_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
        _sut.Delete(FakerKey());
    }

    [Fact(Skip = "Requires tb_trs_dobill_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.GetData(FakerKey());

        actual.Should().NotBeNull();
        actual.PenjualanId.Should().Be(expected.PenjualanId);
        actual.ResepId.Should().Be(expected.ResepId);
        actual.RegId.Should().Be(expected.RegId);
        actual.LayananId.Should().Be(expected.LayananId);
        actual.DokterId.Should().Be(expected.DokterId);
        actual.TipeJaminanId.Should().Be(expected.TipeJaminanId);
        actual.TipeBarangId.Should().Be(expected.TipeBarangId);
        actual.GrandTotal.Should().Be(expected.GrandTotal);
        actual.UserId.Should().Be(expected.UserId);
    }

    [Fact(Skip = "Requires tb_trs_dobill_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.ListData(new RegFilter(expected.RegId)).ToList();

        actual.Should().Contain(x => x.PenjualanId == expected.PenjualanId);
    }

    private sealed record RegFilter(string RegId) : IRegKey;
}
