using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfCreateBaselineTarifPolicyCmd(
    string UserId,
    string? PolicyNo = null,
    string PublishedBy = "") : IRequest<TrfCreateBaselineTarifPolicyResponse>;

public record TrfCreateBaselineTarifPolicyResponse(
    string TarifPolicyId,
    string PolicyNo,
    string PublishLogId,
    int VariantCount);

public class TrfCreateBaselineTarifPolicyHandler
    : IRequestHandler<TrfCreateBaselineTarifPolicyCmd, TrfCreateBaselineTarifPolicyResponse>
{
    private const string PublishLogIdPrefix = "TPL";
    private const string BaselineDescription =
        "Phase-5 baseline anchor imported from current BILRG_NilaiTarif projection.";

    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITarifPublishLogRepo _tarifPublishLogRepo;
    private readonly INilaiTarifProjectionWriter _nilaiTarifProjectionWriter;
    private readonly ITarifProjectionReadRepo _projectionReadRepo;
    private readonly ITarifOperationalStateRepo _operationalStateRepo;
    private readonly ITarifMigrationGuard _migrationGuard;
    private readonly TarifOperationalGate _operationalGate;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ILogger<TrfCreateBaselineTarifPolicyHandler> _logger;
    private readonly ITglJamProvider _tglJamProvider;

    public TrfCreateBaselineTarifPolicyHandler(
        ITarifPolicyRepo tarifPolicyRepo,
        ITarifPublishLogRepo tarifPublishLogRepo,
        INilaiTarifProjectionWriter nilaiTarifProjectionWriter,
        ITarifProjectionReadRepo projectionReadRepo,
        ITarifOperationalStateRepo operationalStateRepo,
        ITarifMigrationGuard migrationGuard,
        TarifOperationalGate operationalGate,
        ITarifRepo tarifRepo,
        ITipeTarifRepo tipeTarifRepo,
        IKelasRepo kelasRepo,
        IKomponenRepo komponenRepo,
        ILogger<TrfCreateBaselineTarifPolicyHandler> logger,
        ITglJamProvider? tglJamProvider = null)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tarifPublishLogRepo = tarifPublishLogRepo;
        _nilaiTarifProjectionWriter = nilaiTarifProjectionWriter;
        _projectionReadRepo = projectionReadRepo;
        _operationalStateRepo = operationalStateRepo;
        _migrationGuard = migrationGuard;
        _operationalGate = operationalGate;
        _tarifRepo = tarifRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _kelasRepo = kelasRepo;
        _komponenRepo = komponenRepo;
        _logger = logger;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrfCreateBaselineTarifPolicyResponse> Handle(
        TrfCreateBaselineTarifPolicyCmd request,
        CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        Guard.Against.NullOrWhiteSpace(request.UserId);
        var publishedBy = string.IsNullOrWhiteSpace(request.PublishedBy)
            ? request.UserId
            : request.PublishedBy;

        _migrationGuard.EnsurePublishAllowed();

        var policyNo = string.IsNullOrWhiteSpace(request.PolicyNo)
            ? $"BASELINE-{occurredAt:yyyyMMdd}"
            : request.PolicyNo.Trim();

        EnsurePolicyNoAvailable(policyNo);

        var projectionRows = _projectionReadRepo.ListAllProjection();
        if (projectionRows.Count == 0)
        {
            throw new InvalidOperationException(
                "BILRG projection kosong; tidak dapat membuat baseline policy.");
        }

        var rowsWithKomponen = projectionRows
            .Where(r => r.Komponen.Count > 0)
            .ToList();
        if (rowsWithKomponen.Count == 0)
        {
            throw new InvalidOperationException(
                "Tidak ada variant dengan baris komponen pada projection; baseline dibatalkan.");
        }

        var policy = TarifPolicyType.Create(
            policyNo,
            "Baseline from BILRG projection",
            occurredAt,
            BaselineDescription,
            request.UserId,
            occurredAt);

        foreach (var row in rowsWithKomponen)
        {
            var komponenLines = row.Komponen
                .Select(k => new TarifVariantKomponenType(
                    k.NoUrut,
                    new KomponenReff(k.KomponenId, ""),
                    k.Nilai))
                .ToList();

            policy = policy.AddVariant(
                row.TarifId,
                row.KelasId,
                row.TipeTarifId,
                row.Nilai,
                komponenLines);
        }

        EnsureMasterReferences(policy.Variants.ToList());

        var publishLogId = NunaId.New(PublishLogIdPrefix);
        var publishedAt = occurredAt;

        _logger.LogInformation(
            "Baseline TarifPolicy creation started for {PolicyNo} with {VariantCount} variants",
            policyNo,
            policy.Variants.Count());

        using var gate = _operationalGate.Acquire(TarifOperation.Publish);
        using var trans = TransHelper.NewScope();
        try
        {
            _tarifPolicyRepo.SaveChanges(policy);

            var snapshotVariants = new List<TarifVariantType>();
            var logDetails = new List<TarifPublishLogDetailType>();

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
            }

            var publishedPolicy = WithVariants(policy, snapshotVariants).MarkPublished(publishedBy, publishedAt);
            var publishLog = new TarifPublishLogType(
                publishLogId,
                policy.TarifPolicyId,
                publishedBy,
                publishedAt,
                snapshotVariants.Count,
                $"BASELINE backfill {policyNo}",
                logDetails);

            _tarifPublishLogRepo.Insert(publishLog);
            _tarifPolicyRepo.SaveChanges(publishedPolicy);
            trans.Complete();

            _operationalStateRepo.RecordBaseline(
                publishedPolicy.TarifPolicyId,
                request.UserId,
                publishedAt);

            _logger.LogInformation(
                "Baseline TarifPolicy {TarifPolicyId} created and published; PublishLogId={PublishLogId}",
                publishedPolicy.TarifPolicyId,
                publishLogId);

            return Task.FromResult(new TrfCreateBaselineTarifPolicyResponse(
                publishedPolicy.TarifPolicyId,
                publishedPolicy.PolicyNo,
                publishLogId,
                snapshotVariants.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Baseline TarifPolicy creation failed for {PolicyNo}", policyNo);
            throw new InvalidOperationException(
                "Baseline policy creation failed; perubahan dibatalkan (transaction rolled back).",
                ex);
        }
    }

    private void EnsurePolicyNoAvailable(string policyNo)
    {
        var exists = _tarifPolicyRepo
            .ListData(new TarifPolicyListFilter(Keyword: policyNo))
            .Any(p => string.Equals(p.PolicyNo, policyNo, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(
                $"Policy dengan PolicyNo '{policyNo}' sudah ada; baseline tidak dijalankan.");
        }
    }

    private void EnsureMasterReferences(IReadOnlyList<TarifVariantType> variants)
    {
        foreach (var variant in variants)
        {
            if (!_tarifRepo.LoadEntity(TarifType.Key(variant.TarifId)).HasValue)
                throw new InvalidOperationException($"Tarif '{variant.TarifId}' tidak ditemukan.");

            if (!_tipeTarifRepo.LoadEntity(TipeTarifType.Key(variant.TipeTarifId)).HasValue)
                throw new InvalidOperationException($"TipeTarif '{variant.TipeTarifId}' tidak ditemukan.");

            if (!_kelasRepo.LoadEntity(KelasType.Key(variant.KelasId)).HasValue)
                throw new InvalidOperationException($"Kelas '{variant.KelasId}' tidak ditemukan.");
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
                throw new InvalidOperationException($"Komponen '{key.KomponenId}' tidak ditemukan.");
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
