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
    public string NoPolis { get; private set; } 
    public string AtasNama { get; private set; } 
    public DateOnly ExpiredDate { get; private set; } 

    public TipeJaminanReff TipeJaminan { get; init; }
    public KelasReff Kelas { get; private set; }
    public bool IsCoverRajal { get; private set; }
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
        
        var statusCoverage = ListCover.Any(x => x.Status == status) ;
        if (statusCoverage)
            throw new ArgumentException("Status Kepesertaan already exist");

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

    public void Update(string noPolis, string atasNama, DateOnly expiredDate, KelasReff kelas, bool isCoverRajal)
    {
        Guard.Against.NullOrWhiteSpace(noPolis, nameof(noPolis));
        Guard.Against.NullOrWhiteSpace(atasNama, nameof(atasNama));
        Guard.Against.Null(kelas, nameof(kelas));

        NoPolis = noPolis;
        AtasNama = atasNama;
        ExpiredDate = expiredDate;
        Kelas = kelas;
        IsCoverRajal = isCoverRajal;
    }

    public void UpdateCoverageStatus(PasienModel pasien, StatusPesertaType newStatus)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(newStatus, nameof(newStatus));

        var cover = _listCover.FirstOrDefault(x => x.Pasien.PasienId == pasien.PasienId);
        if (cover == null)
            throw new ArgumentException("Cover not found");

        if (cover.Status.StatusCode == "P" && newStatus.StatusCode != "P")
            throw new ArgumentException("Status peserta utama tidak dapat diubah");

        if (cover.Status.StatusCode != "P" && cover.Status.StatusCode != newStatus.StatusCode)
        {
            var duplicateStatus = _listCover.Any(x => x.Pasien.PasienId != pasien.PasienId && x.Status.StatusCode == newStatus.StatusCode);
            if (duplicateStatus)
                throw new ArgumentException("Status Kepesertaan already exist");
        }

        var index = _listCover.IndexOf(cover);
        _listCover[index] = cover with { Status = newStatus };
    }

    public void Update(string noPolis, string atasNama, DateOnly expiredDate, KelasReff kelas, bool isCoverRajal, PasienModel pasien, StatusPesertaType newStatus)
    {
        Update(noPolis, atasNama, expiredDate, kelas, isCoverRajal);
        UpdateCoverageStatus(pasien, newStatus);
    }
    #endregion

    public PolisReff ToReff()
        => new PolisReff(PolisId, NoPolis, AtasNama);
}

public record PolisReff(
    string PolisId,
    string NoPolis,
    string AtasName);
