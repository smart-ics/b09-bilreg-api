using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record RegTipeJaminanVo
{
    private const string TIPE_JAMINAN_UMUM = "00000";

    public string TipeJaminanId {get;}
    public string TipeJaminanName {get;}
    public string JaminanId {get;}
    public string JaminanName {get;}
    public string PolisId {get;}
    public string PolisAtasNama {get;}
    public string KelasJaminanId {get;}
    public string KelasJaminanName {get;}
    
    public RegTipeJaminanVo(TipeJaminanModel tipeJaminan, PolisModel? polis)
    {
        TipeJaminanId = tipeJaminan.TipeJaminanId;
        TipeJaminanName = tipeJaminan.TipeJaminanName;
        JaminanId = tipeJaminan.JaminanId;
        JaminanName = tipeJaminan.JaminanName;
        PolisId = polis?.PolisId ?? string.Empty;
        PolisAtasNama = polis?.AtasNama ?? string.Empty;
        KelasJaminanId = polis?.KelasId ?? string.Empty;
        KelasJaminanName = polis?.KelasName ?? string.Empty;

        Validate();

        if (TipeJaminanId != TIPE_JAMINAN_UMUM)
            Guard.IsTrue(polis?.TipeJaminanId == TipeJaminanId);
    }

    public RegTipeJaminanVo(string tipeJaminanId, string tipeJaminanName,
        string jaminanId, string jaminanName,
        string polisId, string polisAtasNama, 
        string kelasJaminanId, string kelasJaminanName)
    {
        TipeJaminanId = tipeJaminanId;
        TipeJaminanName = tipeJaminanName;
        JaminanId = jaminanId;
        JaminanName = jaminanName;
        PolisId = polisId;
        PolisAtasNama = polisAtasNama;
        KelasJaminanId = kelasJaminanId;
        KelasJaminanName = kelasJaminanName;

        Validate();
    }

    private void Validate()
    {
        Guard.IsNotEmpty(TipeJaminanId);
        Guard.IsNotEmpty(TipeJaminanName);

        if (TipeJaminanId == TIPE_JAMINAN_UMUM)
            ValidateUmum();
        else
            ValidateAsuransi();
    }

    private  void ValidateUmum()
    {
        //  pasien umum => data polis harus kosong
        Guard.IsNullOrEmpty(PolisId);
        Guard.IsNullOrEmpty(PolisAtasNama);
        Guard.IsNullOrEmpty(KelasJaminanId);
        Guard.IsNullOrEmpty(KelasJaminanName);
    }

    private void ValidateAsuransi()
    {
        Guard.IsNotNullOrEmpty(PolisId);
        Guard.IsNotNullOrEmpty(PolisAtasNama);
        Guard.IsNotNullOrEmpty(KelasJaminanId);
        Guard.IsNotNullOrEmpty(KelasJaminanName);
    }


};

public class  RegTipeJaminanVoTest
{
    private RegTipeJaminanVo _sut = null!;
    
    [Fact]
    public void T01_GivenJaminanUmum_AndPolisEmpty_WhenCreate_ThenSuccess()
    {
        var tipeJaminan = new TipeJaminanModel("00000", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = null;
        _sut = new RegTipeJaminanVo(tipeJaminan, polis!);
    }

    [Fact]
    public void T02_GivenJaminanUmum_ButPolisNotEmpty_WhenCreate_ThenThrowError()
    {
        var tipeJaminan = new TipeJaminanModel("00000", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = new PolisBuilder("A");
        var actual = () => _sut = new RegTipeJaminanVo(tipeJaminan, polis!);
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
        _sut = new RegTipeJaminanVo(tipeJaminan, polis!);
    }
    
    [Fact]
    public void T04_GivenJaminanAsuransi_ButPolisEmpty_WhenCreate_ThenThrowError()
    {
        var tipeJaminan = new TipeJaminanModel("00001", "A1");
        tipeJaminan.Set(new JaminanModel("B", "B1"));
        PolisModel? polis = null;
        var actual = () => _sut = new RegTipeJaminanVo(tipeJaminan, polis!);
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
        var actual = () => _sut = new RegTipeJaminanVo(tipeJaminan, polis!);
        actual.Should().Throw<ArgumentException>();
    }
}