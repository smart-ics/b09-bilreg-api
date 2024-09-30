using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using CommunityToolkit.Diagnostics;

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
    private const string TIPE_JAMINAN_UMUM = "0000";
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