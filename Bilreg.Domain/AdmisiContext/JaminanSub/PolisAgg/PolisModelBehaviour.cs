using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public partial class PolisModel
{
    public PolisModel(string polisId)
    {
        PolisId = polisId;
    }
    
    // public void SetPolisDetail(string noPolis, string atasName, DateTime expiredDate)
    //     => (NoPolis, AtasNama, ExpiredDate) = (noPolis, atasName, expiredDate);
    //
    // public void WithTipeJaminan(TipeJaminanModel tipeJaminan)
    // {
    //     Guard.IsNotNull(tipeJaminan);
    //     Guard.IsNotEmpty(tipeJaminan.TipeJaminanId);
    //     Guard.IsNotEmpty(tipeJaminan.TipeJaminanName);
    //     
    //     TipeJaminanId = tipeJaminan.TipeJaminanId;
    //     TipeJaminanName = tipeJaminan.TipeJaminanName;
    // }
    //
    // public void WithKelas(KelasModel kelas)
    // {
    //     Guard.IsNotNull(kelas);
    //     Guard.IsNotEmpty(kelas.KelasId);
    //     Guard.IsNotEmpty(kelas.KelasName);
    //     
    //     KelasId = kelas.KelasId;
    //     KelasName = kelas.KelasName;
    // }
    //
    // public void CoverRawatJalan(bool value)
    //     => IsCoverRajal = value;
    //
    // public void AddCover(PasienModel pasien, string status)
    // {
    //     var newCover = new PolisCoverModel(PolisId);
    //     newCover.WithPasien(pasien);
    //     ListCover.Add(newCover);
    // }
    //
    // public void RemoveCover(Predicate<PolisCoverModel> predicate)
    //  => ListCover.RemoveAll(predicate);
    //
    // public void Attach(IEnumerable<PolisCoverModel> listCover)
    //  => ListCover = listCover.ToList();
    //
    // public void SetIsCoverRajal(bool value)
    //     => IsCoverRajal = value;
}

public class PolisBuilder : PolisModel
{
    public PolisBuilder(string polisId) : base(polisId)
    {
        ListCover = [];
    }

    public PolisBuilder(PolisModel polis) : base(polis.PolisId)
    {
        (NoPolis, AtasNama, ExpiredDate) = (polis.NoPolis, polis.AtasNama, polis.ExpiredDate);
        (TipeJaminanId, TipeJaminanName) = (polis.TipeJaminanId, polis.TipeJaminanName);
        (KelasId, KelasName) = (polis.KelasId, polis.KelasName);
        IsCoverRajal = polis.IsCoverRajal;
        ListCover = polis.ListCover;
    }

    public PolisBuilder SetPolisDetail(string noPolis, string atasName, DateTime expiredDate)
    {
        (NoPolis, AtasNama, ExpiredDate) = (noPolis, atasName, expiredDate);
        return this;
    }
    
    public PolisBuilder WithTipeJaminan(TipeJaminanModel tipeJaminan)
    {
        Guard.IsNotNull(tipeJaminan);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanId);
        Guard.IsNotEmpty(tipeJaminan.TipeJaminanName);
        
        TipeJaminanId = tipeJaminan.TipeJaminanId;
        TipeJaminanName = tipeJaminan.TipeJaminanName;

        return this;
    }

    public PolisBuilder WithKelas(KelasModel kelas)
    {
        Guard.IsNotNull(kelas);
        Guard.IsNotEmpty(kelas.KelasId);
        Guard.IsNotEmpty(kelas.KelasName);
        
        KelasId = kelas.KelasId;
        KelasName = kelas.KelasName;

        return this;
    }

    public PolisBuilder CoverRawatJalan(bool value)
    {
        IsCoverRajal = value;
        return this;
    }

    public PolisBuilder AddCover(PasienModel pasien, string status)
    {
        var newCover = new PolisCoverModel(PolisId);
        newCover.WithPasien(pasien);
        ListCover.Add(newCover);
        return this;
    }

    public PolisBuilder RemoveCover(Predicate<PolisCoverModel> predicate)
    {
        ListCover.RemoveAll(predicate);
        return this;
    }

    public PolisBuilder Attach(IEnumerable<PolisCoverModel> listCover)
    {
        ListCover = listCover.ToList();
        return this;

    }

    public PolisBuilder SetIsCoverRajal(bool value)
    {
        IsCoverRajal = value;
        return this;

    }
}