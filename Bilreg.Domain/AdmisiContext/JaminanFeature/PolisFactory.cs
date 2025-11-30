using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Helpers;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

public interface IPolisFactory
{
    PolisModel Create(PasienModel pasien, TipeJaminanType tipeJaminan,
        KelasType kelas, string noPolis, string atasNama, 
        StatusPesertaType statusPeserta, DateOnly expiredDate,
        bool isCoverRajal);
}

public class PolisFactory : IPolisFactory
{
    private readonly ISequencerManual _sequencer;
    private const string SEQUENCE_TAG = "PS-POLIS";
    public PolisFactory(ISequencerManual sequencer)
    {
        _sequencer = sequencer;
    }

    public PolisModel Create(PasienModel pasien, TipeJaminanType tipeJaminan, 
        KelasType kelas, string noPolis, string atasNama, 
        StatusPesertaType statusPeserta, DateOnly expiredDate,
        bool isCoverRajal)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(tipeJaminan, nameof(tipeJaminan));
        Guard.Against.Null(kelas, nameof(kelas));
        Guard.Against.NullOrEmpty(noPolis, nameof(noPolis));
        Guard.Against.NullOrEmpty(atasNama, nameof(atasNama));
        Guard.Against.Null(statusPeserta, nameof(statusPeserta));
            
        var newId = _sequencer.GetNextNoUrut(SEQUENCE_TAG, "");
        var polisId = $"PS{newId:D8}";
        var polis = new PolisModel(polisId, noPolis, atasNama, 
            tipeJaminan.ToReff(), kelas.ToReff(), expiredDate, 
            isCoverRajal, []);
        polis.AddCoverage(pasien, statusPeserta);
        return polis;
    }
}