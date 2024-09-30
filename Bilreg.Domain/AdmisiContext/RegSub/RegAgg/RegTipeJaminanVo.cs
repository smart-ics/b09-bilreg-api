using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record RegTipeJaminanVo(
    string TipeJaminanId,
    string TipeJaminanName,
    string JaminanId,
    string JaminanName,
    string PolisId,
    string PolisAtasNama,
    string KelasJaminanId,
    string KelasJaminanName)
{
    private const string TIPE_JAMINAN_UMUM = "00000";
    public static RegTipeJaminanVo Create(TipeJaminanModel tipeJaminan, PolisModel polis)
    {
        //  GUARD
        Guard.IsNotNull(tipeJaminan);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanId);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanName);
        Guard.IsNotEmpty(tipeJaminan.JaminanId);
        Guard.IsNotEmpty(tipeJaminan.JaminanName);

        //  Px Jaminan harus punya polis
        //  Px Umum harus tidak punya polis
        var isPasienUmum = tipeJaminan.TipeJaminanId == TIPE_JAMINAN_UMUM;
        var isPolisEmpty = polis is null || polis.PolisId.Trim() == string.Empty;
        if (isPasienUmum ^ isPolisEmpty)
            throw new ArgumentException($"Tipe Jaminan and Polis is invalid");

        if (isPasienUmum)
            return new RegTipeJaminanVo(
                tipeJaminan.TipeJaminanId, tipeJaminan.TipeJaminanName,
                tipeJaminan.JaminanId, tipeJaminan.JaminanName, 
                "", "", "", "");
        
        Guard.IsNotNull(polis);
        Guard.IsNotEmpty(polis.PolisId);
        Guard.IsNotEmpty(polis.AtasNama);
        Guard.IsNotEmpty(polis.KelasId);
        Guard.IsNotEmpty(polis.KelasName);

        if (polis.TipeJaminanId != tipeJaminan.TipeJaminanId)
            throw new ArgumentException($"Tipe Jaminan Polis invalid");

        return new RegTipeJaminanVo(
            tipeJaminan.TipeJaminanId, tipeJaminan.TipeJaminanName,
            tipeJaminan.JaminanId, tipeJaminan.JaminanName,
            polis.PolisId, polis.AtasNama, polis.KelasId, polis.KelasName);
    }

    protected static RegTipeJaminanVo Load(string tipeJaminanId, string tipeJaminanName,
        string jaminanId, string jaminanName,
        string polisId, string polisAtasNama,
        string kelasJaminanId, string kelasJaminanName)
    {
        return new RegTipeJaminanVo(
            tipeJaminanId,
            tipeJaminanName,
            jaminanId,
            jaminanName,
            polisId,
            polisAtasNama,
            kelasJaminanId,
            kelasJaminanName);
    }
};

public class  RegTipeJaminanVoTest
{
    private RegTipeJaminanVo _sut;
    
    [Fact]
    public void T01_GivenJaminanUmum_AndPolisEmpty_WhenCreate_ThenSuccess()
    {
        var tipeJaminan = new TipeJaminanModel("00000", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = null;
        _sut = RegTipeJaminanVo.Create(tipeJaminan, polis!);
    }

    [Fact]
    public void T02_GivenJaminanUmum_ButPolisNotEmpty_WhenCreate_ThenThrowError()
    {
        var tipeJaminan = new TipeJaminanModel("00000", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = new PolisBuilder("A");
        var actual = () => _sut = RegTipeJaminanVo.Create(tipeJaminan, polis!);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenJaminanAsuransi_AndPolisNotEmpty_WhenCreate_ThenSuccess()
    {
        var tipeJaminan = new TipeJaminanModel("00001", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = new PolisBuilder("A")
            .SetPolisDetail("A1", "A2", new DateTime(3000,1,1))
            .WithKelas(new KelasModel("A3", "A4"))
            .WithTipeJaminan(new TipeJaminanModel("00001", "A1"));
        _sut = RegTipeJaminanVo.Create(tipeJaminan, polis!);
    }
    
    [Fact]
    public void T04_GivenJaminanAsuransi_ButPolisEmpty_WhenCreate_ThenThrowError()
    {
        var tipeJaminan = new TipeJaminanModel("00001", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = null;
        var actual = () => _sut = RegTipeJaminanVo.Create(tipeJaminan, polis!);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T05_GivenJaminanAsuransi_ButTipeJaminanPolisDifferent_WhenCreate_ThenThrowError()
    {
        var tipeJaminan = new TipeJaminanModel("00001", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = new PolisBuilder("A")
            .SetPolisDetail("A1", "A2", new DateTime(3000,1,1))
            .WithKelas(new KelasModel("A3", "A4"))
            .WithTipeJaminan(new TipeJaminanModel("00002", "A2"));
        var actual = () => _sut = RegTipeJaminanVo.Create(tipeJaminan, polis!);
        actual.Should().Throw<ArgumentException>();
    }
}