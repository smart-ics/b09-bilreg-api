using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

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

    public void SaveChanges(PakaiBedAlokasiModel alokasi)
    {
        var legacyDto = _legacyDal.GetData(PakaiBedModel.Key(alokasi.PakaiBedId));
        if (legacyDto is null)
            throw PakaiBedPersistenceException.Integrity(
                $"Header RNA PakaiBed {alokasi.PakaiBedId} tidak memiliki pasangan ta_trs_bed.");

        SaveChanges(alokasi, legacyDto.ToModel());
    }

    public void SaveChanges(PakaiBedAlokasiModel alokasi, PakaiBedModel legacyPakaiBed)
    {
        ValidateComposite(alokasi, legacyPakaiBed);

        using var trans = TransHelper.NewScope();

        var storedHeader = _alokasiDal.GetData(alokasi);
        var storedLegacy = _legacyDal.GetData(legacyPakaiBed);
        var headerDto = PakaiBedAlokasiDto.FromModel(alokasi);
        var legacyDto = ProjectLegacyDto(alokasi, legacyPakaiBed);

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

        trans.Complete();
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
            .Select(PakaiBedTransisiDto.FromModel)
            .ToList();
        if (rows.Count != 0)
            _transisiDal.Insert(rows);
    }

    private void InsertNewCorrections(PakaiBedAlokasiModel model)
    {
        var existingIds = (_koreksiDal.ListData(model) ?? [])
            .Select(x => x.CorrectionId).ToHashSet(StringComparer.Ordinal);
        var rows = model.ListCorrection
            .Where(x => !existingIds.Contains(x.CorrectionId))
            .Select(PakaiBedKoreksiDto.FromModel)
            .ToList();
        if (rows.Count != 0)
            _koreksiDal.Insert(rows);
    }

    private static void ValidateComposite(PakaiBedAlokasiModel alokasi, PakaiBedModel legacy)
    {
        if (alokasi.PakaiBedStatus is not (PakaiBedStatusEnum.Active or PakaiBedStatusEnum.Released or PakaiBedStatusEnum.Cancelled)
            || alokasi.StartedAt is null)
            throw PakaiBedPersistenceException.Integrity(
                "Hanya alokasi PakaiBed berstatus Active, Released, atau Cancelled yang boleh dipersistensikan.");

        var mismatches = new List<string>();
        if (alokasi.PakaiBedId != legacy.PakaiBedId) mismatches.Add(nameof(alokasi.PakaiBedId));
        if (alokasi.RegId != legacy.Reg.RegId) mismatches.Add(nameof(alokasi.RegId));
        if (alokasi.PasienId != legacy.Reg.PasienId) mismatches.Add(nameof(alokasi.PasienId));
        if (alokasi.BedId != legacy.Bed.BedId) mismatches.Add(nameof(alokasi.BedId));
        if (!AreDatesClose(alokasi.StartedAt.Value, legacy.Periode.Masuk.Timestamp)) mismatches.Add(nameof(alokasi.StartedAt));
        if (alokasi.AssignedBy != legacy.Periode.Masuk.UserId) mismatches.Add(nameof(alokasi.AssignedBy));

        if (alokasi.PakaiBedStatus == PakaiBedStatusEnum.Active)
        {
            if (IsLegacyVoided(legacy))
                mismatches.Add(nameof(legacy.AuditTrail.Voided));
            if (IsLegacyOut(legacy))
                mismatches.Add(nameof(legacy.Periode.Keluar));
        }
        else if (alokasi.PakaiBedStatus == PakaiBedStatusEnum.Released)
        {
            if (IsLegacyVoided(legacy))
                mismatches.Add(nameof(legacy.AuditTrail.Voided));

            if (IsLegacyOut(legacy))
            {
                var releaseTransition = alokasi.ListTransition.LastOrDefault(x => x.PakaiBedTransition == PakaiBedTransisiEnum.Released);
                if (releaseTransition is not null && !AreDatesClose(releaseTransition.OccurredAt, legacy.Periode.Keluar.Timestamp))
                    mismatches.Add(nameof(legacy.Periode.Keluar));
            }
        }
        else if (alokasi.PakaiBedStatus == PakaiBedStatusEnum.Cancelled)
        {
            if (IsLegacyVoided(legacy))
            {
                var voidTransition = alokasi.ListTransition.LastOrDefault(x => x.PakaiBedTransition == PakaiBedTransisiEnum.Cancelled);
                if (voidTransition is not null && HasMeaningfulAuditInfo(legacy.AuditTrail.Voided) &&
                    !AreDatesClose(voidTransition.OccurredAt, legacy.AuditTrail.Voided.Timestamp))
                    mismatches.Add(nameof(legacy.AuditTrail.Voided));
            }
        }

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

    private static PakaiBedDto ProjectLegacyDto(PakaiBedAlokasiModel alokasi, PakaiBedModel legacyPakaiBed)
    {
        var legacyDto = PakaiBedDto.FromModel(legacyPakaiBed);

        if (!string.IsNullOrWhiteSpace(alokasi.BillingKelasId))
        {
            legacyDto = legacyDto with
            {
                fs_kd_kelas = alokasi.BillingKelasId,
                fn_tarif = alokasi.Tarif
            };
        }
        else if (alokasi.Tarif > 0)
        {
            legacyDto = legacyDto with
            {
                fn_tarif = alokasi.Tarif
            };
        }

        if (alokasi.PakaiBedStatus == PakaiBedStatusEnum.Released)
        {
            var releaseTransition = alokasi.ListTransition.LastOrDefault(x => x.PakaiBedTransition == PakaiBedTransisiEnum.Released);
            if (releaseTransition is not null)
            {
                legacyDto = legacyDto with
                {
                    fd_tgl_out = releaseTransition.OccurredAt.ToString(DateFormatEnum.YMD),
                    fs_jam_out = releaseTransition.OccurredAt.ToString(DateFormatEnum.HMS),
                    fs_kd_petugas_out = string.IsNullOrWhiteSpace(releaseTransition.ActorId)
                        ? legacyPakaiBed.Periode.Keluar.UserId
                        : releaseTransition.ActorId
                };
            }
            else if (HasMeaningfulAuditInfo(legacyPakaiBed.Periode.Keluar))
            {
                legacyDto = legacyDto with
                {
                    fd_tgl_out = legacyPakaiBed.Periode.Keluar.Timestamp.ToString(DateFormatEnum.YMD),
                    fs_jam_out = legacyPakaiBed.Periode.Keluar.Timestamp.ToString(DateFormatEnum.HMS),
                    fs_kd_petugas_out = legacyPakaiBed.Periode.Keluar.UserId
                };
            }
        }
        else if (alokasi.PakaiBedStatus == PakaiBedStatusEnum.Cancelled)
        {
            var voidTransition = alokasi.ListTransition.LastOrDefault(x => x.PakaiBedTransition == PakaiBedTransisiEnum.Cancelled);
            if (voidTransition is not null)
            {
                legacyDto = legacyDto with
                {
                    fd_tgl_void = voidTransition.OccurredAt.ToString(DateFormatEnum.YMD),
                    fs_jam_void = voidTransition.OccurredAt.ToString(DateFormatEnum.HMS),
                    fs_kd_petugas_void = string.IsNullOrWhiteSpace(voidTransition.ActorId)
                        ? legacyPakaiBed.AuditTrail.Voided.UserId
                        : voidTransition.ActorId
                };
            }
            else if (IsLegacyVoided(legacyPakaiBed))
            {
                legacyDto = legacyDto with
                {
                    fd_tgl_void = legacyPakaiBed.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.YMD),
                    fs_jam_void = legacyPakaiBed.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.HMS),
                    fs_kd_petugas_void = legacyPakaiBed.AuditTrail.Voided.UserId
                };
            }
        }

        return legacyDto;
    }

    private static bool AreDatesClose(DateTime a, DateTime b) =>
        Math.Abs((a - b).TotalSeconds) < 1;

    private static bool HasMeaningfulAuditInfo(AuditInfoType? info) =>
        info is not null &&
        info != AuditInfoType.Default &&
        info.Timestamp.Year < 3000;

    private static bool IsLegacyVoided(PakaiBedModel legacy) =>
        legacy.AuditTrail.IsVoided || HasMeaningfulAuditInfo(legacy.AuditTrail.Voided);

    private static bool IsLegacyOut(PakaiBedModel legacy) =>
        HasMeaningfulAuditInfo(legacy.Periode.Keluar);
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
