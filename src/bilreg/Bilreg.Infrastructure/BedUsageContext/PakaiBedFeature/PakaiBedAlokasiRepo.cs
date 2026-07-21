using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public sealed class PakaiBedAlokasiRepo : IPakaiBedAlokasiRepo
{
    private readonly IPakaiBedAlokasiDal _alokasiDal;
    private readonly IPakaiBedTransisiDal _transisiDal;
    private readonly IPakaiBedKoreksiDal _koreksiDal;
    private readonly IPakaiBedDal _legacyDal;

    public PakaiBedAlokasiRepo(
        IPakaiBedAlokasiDal alokasiDal,
        IPakaiBedTransisiDal transisiDal,
        IPakaiBedKoreksiDal koreksiDal,
        IPakaiBedDal legacyDal)
    {
        _alokasiDal = alokasiDal;
        _transisiDal = transisiDal;
        _koreksiDal = koreksiDal;
        _legacyDal = legacyDal;
    }

    public void SaveChanges(PakaiBedAlokasiModel alokasi, PakaiBedModel legacyPakaiBed)
    {
        ValidateComposite(alokasi, legacyPakaiBed);

        var storedHeader = _alokasiDal.GetData(alokasi);
        var storedLegacy = _legacyDal.GetData(legacyPakaiBed);
        var headerDto = PakaiBedAlokasiDto.FromModel(alokasi);
        var legacyDto = PakaiBedDto.FromModel(legacyPakaiBed);

        if (storedHeader is not null && alokasi.Version != storedHeader.Version + 1)
            throw PakaiBedPersistenceException.Concurrency(
                $"Versi PakaiBed {alokasi.PakaiBedId} harus {storedHeader.Version + 1}, bukan {alokasi.Version}.");

        if (storedLegacy is null)
            _legacyDal.Insert(legacyDto);
        else
            _legacyDal.Update(legacyDto);

        if (storedHeader is null)
            _alokasiDal.Insert(headerDto);
        else if (_alokasiDal.UpdateConditional(headerDto, storedHeader.Version) != 1)
            throw PakaiBedPersistenceException.Concurrency(
                $"PakaiBed {alokasi.PakaiBedId} telah diubah oleh proses lain.");

        InsertNewTransitions(alokasi);
        InsertNewCorrections(alokasi);
    }

    public MayBe<PakaiBedAlokasiModel> LoadEntity(IPakaiBedAlokasiKey key)
    {
        var header = _alokasiDal.GetData(key);
        if (header is null)
            return MayBe<PakaiBedAlokasiModel>.None;

        var legacy = _legacyDal.GetData(PakaiBedModel.Key(key.PakaiBedId));
        if (legacy is null)
            throw PakaiBedPersistenceException.Integrity(
                $"Header RNA PakaiBed {key.PakaiBedId} tidak memiliki pasangan ta_trs_bed.");

        var transitions = _transisiDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        var corrections = _koreksiDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        var model = header.ToModel(transitions, corrections);
        ValidateLoadedComposite(model, legacy.ToModel());
        return MayBe.From(model);
    }

    private void InsertNewTransitions(PakaiBedAlokasiModel model)
    {
        var existingIds = (_transisiDal.ListData(model) ?? [])
            .Select(x => x.TransitionId).ToHashSet(StringComparer.Ordinal);
        var rows = model.ListTransition
            .Where(x => !existingIds.Contains(x.TransitionId))
            .Select(PakaiBedTransisiDto.FromModel);
        _transisiDal.Insert(rows);
    }

    private void InsertNewCorrections(PakaiBedAlokasiModel model)
    {
        var existingIds = (_koreksiDal.ListData(model) ?? [])
            .Select(x => x.CorrectionId).ToHashSet(StringComparer.Ordinal);
        var rows = model.ListCorrection
            .Where(x => !existingIds.Contains(x.CorrectionId))
            .Select(PakaiBedKoreksiDto.FromModel);
        _koreksiDal.Insert(rows);
    }

    private static void ValidateComposite(PakaiBedAlokasiModel alokasi, PakaiBedModel legacy)
    {
        if (alokasi.PakaiBedStatus != PakaiBedStatusEnum.Active || alokasi.StartedAt is null)
            throw PakaiBedPersistenceException.Integrity(
                "Hanya alokasi PakaiBed berstatus Active yang boleh dipersistensikan.");

        var mismatches = new List<string>();
        if (alokasi.PakaiBedId != legacy.PakaiBedId) mismatches.Add(nameof(alokasi.PakaiBedId));
        if (alokasi.RegId != legacy.Reg.RegId) mismatches.Add(nameof(alokasi.RegId));
        if (alokasi.PasienId != legacy.Reg.PasienId) mismatches.Add(nameof(alokasi.PasienId));
        if (alokasi.BedId != legacy.Bed.BedId) mismatches.Add(nameof(alokasi.BedId));
        if (alokasi.StartedAt.Value != legacy.Periode.Masuk.Timestamp) mismatches.Add(nameof(alokasi.StartedAt));
        if (alokasi.AssignedBy != legacy.Periode.Masuk.UserId) mismatches.Add(nameof(alokasi.AssignedBy));

        if (mismatches.Count != 0)
            throw PakaiBedPersistenceException.Integrity(
                $"Model RNA dan legacy tidak konsisten pada: {string.Join(", ", mismatches)}.");
    }

    private static void ValidateLoadedComposite(PakaiBedAlokasiModel alokasi, PakaiBedModel legacy)
    {
        try
        {
            ValidateComposite(alokasi, legacy);
        }
        catch (PakaiBedPersistenceException ex)
        {
            throw PakaiBedPersistenceException.Integrity(
                $"Data PakaiBed {alokasi.PakaiBedId} tidak konsisten saat dimuat. {ex.Message}");
        }
    }
}

public sealed class PakaiBedPersistenceException : InvalidOperationException
{
    private PakaiBedPersistenceException(string code, string message) : base(message) => Code = code;
    public string Code { get; }

    public static PakaiBedPersistenceException Integrity(string message) =>
        new("INTEGRITY_CONFLICT", message);

    public static PakaiBedPersistenceException Concurrency(string message) =>
        new("CONCURRENCY_CONFLICT", message);
}
