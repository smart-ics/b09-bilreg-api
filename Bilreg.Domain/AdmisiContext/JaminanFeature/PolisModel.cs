using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

public class PolisModel : IPolisKey
{
    private readonly List<PolisCoverModel> _listCover;

    #region CREATION
    public PolisModel(string polisId, string noPolis, string atasNama, 
        TipeJaminanReff tipeJaminan, KelasReff kelas, DateOnly expiredDate,
        bool isCoverRajal, List<PolisCoverModel> listCover)
    {
        PolisId = polisId;
        NoPolis = noPolis;
        AtasNama = atasNama;
        TipeJaminan = tipeJaminan;
        Kelas = kelas;
        ExpiredDate = expiredDate;
        IsCoverRajal = isCoverRajal;
        _listCover = listCover;
    }

    public static PolisModel Default => new PolisModel("-", "-", "-",
        TipeJaminanType.Default.ToReff(), KelasType.Default.ToReff(), 
        new DateOnly(3000,1,1), false, []);

    public static IPolisKey Key(string id) => new PolisModel(id, "-", "-",
        TipeJaminanType.Default.ToReff(), KelasType.Default.ToReff(),
        new DateOnly(3000,1,1), false, []);
    #endregion
    
    #region PROPERTIES
    public string PolisId { get; init; }
    public string NoPolis { get; init; } 
    public string AtasNama { get; init; } 
    public DateOnly ExpiredDate { get; init; } 

    public TipeJaminanReff TipeJaminan { get; init; }
    public KelasReff Kelas { get; init; }
    public bool IsCoverRajal { get; init; }
    public IEnumerable<PolisCoverModel> ListCover => _listCover;
    #endregion
    
    #region BEHAVIOUR
    public void AddCoverage(PasienModel pasien, StatusPesertaType status)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(status, nameof(status));
        
        var duplicate = _listCover.FirstOrDefault(x => x.Pasien.PasienId == pasien.PasienId);
        if (duplicate != null)
            throw new ArgumentException("Cover already exists");
        var newCoverage = new PolisCoverModel(PolisId, pasien.ToReff(), status);
        _listCover.Add(newCoverage);
    }
    
    public void RemoveCoverage(PasienModel pasien)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        
        var cover = _listCover.FirstOrDefault(x => x.Pasien.PasienId == pasien.PasienId);
        if (cover == null)
            throw new ArgumentException("Cover not found");
        if (cover.Status.StatusCode == "P")
            throw new KeyNotFoundException($"Pasien ini adalah peserta utama di polis {cover.PolisId}");
        _listCover.Remove(cover);
    }
    #endregion

    public PolisReff ToReff()
        => new PolisReff(PolisId, NoPolis, AtasNama);
}

public record PolisReff(
    string PolisId,
    string NoPolis,
    string AtasName);
