using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfPublishTarifPolicyCmd(
    string TarifPolicyId,
    string PublishedBy,
    string Note = "") : IRequest<TrfPublishTarifPolicyResponse>, ITarifPolicyKey;

public record TrfPublishTarifPolicyVariantResult(int ItemNo, string NilaiTarifId);

public record TrfPublishTarifPolicyResponse(
    string PublishLogId,
    string TarifPolicyId,
    TarifPolicyStatus PolicyStatus,
    int VariantCount,
    IReadOnlyList<TrfPublishTarifPolicyVariantResult> Variants);

public class TrfPublishTarifPolicyHandler : IRequestHandler<TrfPublishTarifPolicyCmd, TrfPublishTarifPolicyResponse>
{
    private const string PublishLogIdPrefix = "TPL";

    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITarifPublishLogRepo _tarifPublishLogRepo;
    private readonly INilaiTarifProjectionWriter _nilaiTarifProjectionWriter;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITarifMigrationGuard _migrationGuard;
    private readonly TarifOperationalGate _operationalGate;
    private readonly ILogger<TrfPublishTarifPolicyHandler> _logger;

    public TrfPublishTarifPolicyHandler(
        ITarifPolicyRepo tarifPolicyRepo,
        ITarifPublishLogRepo tarifPublishLogRepo,
        INilaiTarifProjectionWriter nilaiTarifProjectionWriter,
        ITarifRepo tarifRepo,
        ITipeTarifRepo tipeTarifRepo,
        IKelasRepo kelasRepo,
        IKomponenRepo komponenRepo,
        ITarifMigrationGuard migrationGuard,
        TarifOperationalGate operationalGate,
        ILogger<TrfPublishTarifPolicyHandler> logger)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tarifPublishLogRepo = tarifPublishLogRepo;
        _nilaiTarifProjectionWriter = nilaiTarifProjectionWriter;
        _tarifRepo = tarifRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _kelasRepo = kelasRepo;
        _komponenRepo = komponenRepo;
        _migrationGuard = migrationGuard;
        _operationalGate = operationalGate;
        _logger = logger;
    }

    public Task<TrfPublishTarifPolicyResponse> Handle(
        TrfPublishTarifPolicyCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TarifPolicyId, nameof(request.TarifPolicyId));
        Guard.Against.NullOrWhiteSpace(request.PublishedBy, nameof(request.PublishedBy));

        _migrationGuard.EnsurePublishAllowed();

        var policy = _tarifPolicyRepo.LoadEntity(request)
            .Match(
                onSome: p => p,
                onNone: () => throw new KeyNotFoundException(
                    $"TarifPolicy '{request.TarifPolicyId}' tidak ditemukan."));

        EnsurePublishable(policy);

        var isRepublish = policy.PolicyStatus == TarifPolicyStatus.Published;
        var publishLogId = NunaId.New(PublishLogIdPrefix);
        var publishedAt = DateTime.Now;

        _logger.LogInformation(
            "TarifPolicy publish started for {TarifPolicyId} (republish={IsRepublish})",
            policy.TarifPolicyId,
            isRepublish);

        using var gate = _operationalGate.Acquire(TarifOperation.Publish);
        using var trans = TransHelper.NewScope();
        try
        {
            var snapshotVariants = new List<TarifVariantType>();
            var logDetails = new List<TarifPublishLogDetailType>();
            var variantResults = new List<TrfPublishTarifPolicyVariantResult>();

            foreach (var variant in policy.Variants.OrderBy(v => v.ItemNo))
            {
                var projection = CreateProjection(variant);
                var nilaiTarifId = _nilaiTarifProjectionWriter.Upsert(projection, policy.TarifPolicyId);
                var snapshot = variant.ToPublishedSnapshot(nilaiTarifId);

                snapshotVariants.Add(snapshot);
                logDetails.Add(new TarifPublishLogDetailType(
                    snapshot.ItemNo,
                    snapshot.TarifId,
                    snapshot.KelasId,
                    snapshot.TipeTarifId,
                    nilaiTarifId,
                    snapshot.Nilai));
                variantResults.Add(new TrfPublishTarifPolicyVariantResult(snapshot.ItemNo, nilaiTarifId));
            }

            var policyWithSnapshots = WithVariants(policy, snapshotVariants);
            var policyToSave = isRepublish
                ? policyWithSnapshots
                : policyWithSnapshots.MarkPublished(request.PublishedBy);

            var publishLog = new TarifPublishLogType(
                publishLogId,
                policy.TarifPolicyId,
                request.PublishedBy,
                publishedAt,
                snapshotVariants.Count,
                request.Note ?? "",
                logDetails);

            _tarifPublishLogRepo.Insert(publishLog);
            _tarifPolicyRepo.SaveChanges(policyToSave);
            trans.Complete();

            _logger.LogInformation(
                "TarifPolicy publish succeeded for {TarifPolicyId}; PublishLogId={PublishLogId}; variants={VariantCount}",
                policy.TarifPolicyId,
                publishLogId,
                snapshotVariants.Count);

            return Task.FromResult(new TrfPublishTarifPolicyResponse(
                publishLogId,
                policyToSave.TarifPolicyId,
                policyToSave.PolicyStatus,
                snapshotVariants.Count,
                variantResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TarifPolicy publish failed for {TarifPolicyId}; transaction rolled back",
                policy.TarifPolicyId);
            throw new InvalidOperationException(
                "TarifPolicy publish failed; projection and policy unchanged (transaction rolled back).",
                ex);
        }
    }

    private void EnsurePublishable(TarifPolicyType policy)
    {
        var variants = policy.Variants.ToList();

        if (variants.Count == 0)
        {
            throw new InvalidOperationException(
                $"TarifPolicy {policy.TarifPolicyId} tidak memiliki variant.");
        }

        var duplicateGroup = variants
            .GroupBy(v => v.VariantCompositeKey)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateGroup is not null)
        {
            var (tarifId, kelasId, tipeTarifId) = duplicateGroup.Key;
            throw new InvalidOperationException(
                $"Variant duplikat untuk kombinasi Tarif={tarifId}, Kelas={kelasId}, TipeTarif={tipeTarifId}.");
        }

        var isFirstPublish = policy.PolicyStatus is TarifPolicyStatus.Draft or TarifPolicyStatus.Reviewed;
        var isRepublish = policy.PolicyStatus == TarifPolicyStatus.Published;

        if (!isFirstPublish && !isRepublish)
        {
            throw new InvalidOperationException(
                $"TarifPolicy {policy.TarifPolicyId} berstatus {policy.PolicyStatus}; publish tidak diperbolehkan.");
        }

        if (isFirstPublish)
            policy.ValidateForPublish();
        else
            policy.ValidateForRepublish();

        EnsureMasterReferences(variants);
    }

    private void EnsureMasterReferences(IReadOnlyList<TarifVariantType> variants)
    {
        foreach (var variant in variants)
        {
            if (!_tarifRepo.LoadEntity(TarifType.Key(variant.TarifId)).HasValue)
            {
                throw new InvalidOperationException($"Tarif '{variant.TarifId}' tidak ditemukan.");
            }

            if (!_tipeTarifRepo.LoadEntity(TipeTarifType.Key(variant.TipeTarifId)).HasValue)
            {
                throw new InvalidOperationException($"TipeTarif '{variant.TipeTarifId}' tidak ditemukan.");
            }

            if (!_kelasRepo.LoadEntity(KelasType.Key(variant.KelasId)).HasValue)
            {
                throw new InvalidOperationException($"Kelas '{variant.KelasId}' tidak ditemukan.");
            }
        }

        var komponenKeys = variants
            .SelectMany(v => v.ListKomponen)
            .Select(k => k.Komponen)
            .DistinctBy(k => k.KomponenId)
            .ToList();

        if (komponenKeys.Count == 0)
            return;

        var masters = _komponenRepo.ListData(komponenKeys)?.ToList() ?? [];
        foreach (var key in komponenKeys)
        {
            if (masters.All(m => m.KomponenId != key.KomponenId))
            {
                throw new InvalidOperationException($"Komponen '{key.KomponenId}' tidak ditemukan.");
            }
        }
    }

    private NilaiTarifType CreateProjection(TarifVariantType variant)
    {
        var tarif = _tarifRepo.LoadEntity(TarifType.Key(variant.TarifId))
            .GetValueOrThrow($"Tarif '{variant.TarifId}' tidak ditemukan.");
        var tipeTarif = _tipeTarifRepo.LoadEntity(TipeTarifType.Key(variant.TipeTarifId))
            .GetValueOrThrow($"TipeTarif '{variant.TipeTarifId}' tidak ditemukan.");
        var kelas = _kelasRepo.LoadEntity(KelasType.Key(variant.KelasId))
            .GetValueOrThrow($"Kelas '{variant.KelasId}' tidak ditemukan.");

        var komponenKeys = variant.ListKomponen.Select(x => x.Komponen).ToList();
        var komponenMasters = komponenKeys.Count == 0
            ? []
            : _komponenRepo.ListData(komponenKeys)?.ToList() ?? [];

        var projectionKomponen = variant.ListKomponen
            .Select(line =>
            {
                var master = komponenMasters.FirstOrDefault(m => m.KomponenId == line.Komponen.KomponenId);
                var reff = master?.ToReff() ?? line.Komponen;
                return new NilaiTarifKomponenType(line.NoUrut, reff, line.Nilai);
            })
            .ToList();

        return new NilaiTarifType(
            "-",
            variant.TarifId,
            tarif.TarifName,
            tipeTarif.ToReff(),
            kelas.ToReff(),
            variant.Nilai,
            projectionKomponen);
    }

    private static TarifPolicyType WithVariants(
        TarifPolicyType policy,
        IReadOnlyList<TarifVariantType> variants) =>
        new(
            policy.TarifPolicyId,
            policy.PolicyNo,
            policy.PolicyName,
            policy.EffectiveDateInfo,
            policy.Description,
            policy.PolicyStatus,
            policy.AuditTrail,
            variants);
}
