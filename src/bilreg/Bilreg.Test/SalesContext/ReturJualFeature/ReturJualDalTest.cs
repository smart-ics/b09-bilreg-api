using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.SalesContext.ReturJualFeature;

public class ReturJualDalTest
{
    private readonly ReturJualDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ReturJualDto FakerData() => new()
    {
        ReturJualId = "RUTST00001",
        TglJam = new DateTime(2026, 8, 6, 10, 0, 0),
        UserId = "U1",
        PenjualanId = "DUTST00001",
        PenjualanDate = new DateTime(2026, 8, 6, 9, 0, 0),
        RegId = "RG00000001",
        PasienId = "MR0001",
        PasienName = "Pasien Tes",
        Reason = "Barang rusak",
        LayananId = "LYJ01",
        LayananName = "Apotek RJ",
        TipeJaminanId = "00000",
        TipeJaminanName = "Umum",
        TipeBrgId = "01",
        TipeBrgName = "Obat",
        SumSubTotalJual = 10000,
        SumSubTotalRetur = 3600,
        SumTax = 40,
        Pembulatan = 0,
        GrandTotal = 3640,
        TglVoid = "3000-01-01",
        JamVoid = "00:00:00",
        UserVoidId = "-"
    };

    private static IReturJualKey FakerKey() => ReturJualModel.Key("RUTST00001");

    [Fact(Skip = "Requires tb_trs_rjual_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var data = FakerData();
        _sut.Insert(data);

        data.GrandTotal = 3500;
        _sut.Update(data);
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
        _sut.Delete(FakerKey());
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.GetData(FakerKey());

        actual.Should().NotBeNull();
        actual.ReturJualId.Should().Be(expected.ReturJualId);
        actual.PenjualanId.Should().Be(expected.PenjualanId);
        actual.RegId.Should().Be(expected.RegId);
        actual.Reason.Should().Be(expected.Reason);
        actual.GrandTotal.Should().Be(expected.GrandTotal);
    }

    [Fact(Skip = "Requires tb_trs_rjual_umum in test database (deploy Bilreg.SqlDb SalesContext scripts)")]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.ListData(new RegFilter(expected.RegId)).ToList();

        actual.Should().Contain(x => x.ReturJualId == expected.ReturJualId);
    }

    private sealed record RegFilter(string RegId) : IRegKey;
}
