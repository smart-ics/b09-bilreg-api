using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Handler untuk query worklist antrean aktif petugas farmasi per depo.
/// Mendukung filter tab:
/// - "Menunggu" (TAKEN)
/// - "Menunggu Telaah" (PENERIMAAN)
/// - "Dilewati" (DILEWATI)
/// - "Siap Penjualan" (Telaah disetujui, siap konfirmasi biaya di Loket 1)
/// - "Siap Serah" (SIAP_SERAH, Loket 2)
/// </summary>
public class PharmacyQueueWorklistHandler : IRequestHandler<PharmacyQueueWorklistQry, PharmacyQueueWorklistResponse>
{
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IResepRepo? _resepRepo;
    private readonly IRegRepo? _regRepo;
    private readonly ITelaahResepRepo? _telaahRepo;

    public PharmacyQueueWorklistHandler(
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IPharmacyQueueMappingRepo mappingRepo,
        IResepRepo? resepRepo = null,
        IRegRepo? regRepo = null,
        ITelaahResepRepo? telaahRepo = null)
    {
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _mappingRepo = mappingRepo;
        _resepRepo = resepRepo;
        _regRepo = regRepo;
        _telaahRepo = telaahRepo;
    }

    public Task<PharmacyQueueWorklistResponse> Handle(
        PharmacyQueueWorklistQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DepoFarmasiId, nameof(request.DepoFarmasiId));

        var activeEntries = _queueEntryRepo.ListActiveByDepo(request.DepoFarmasiId);

        var servicePoints = _servicePointRepo.ListByDepo(request.DepoFarmasiId)
            .ToDictionary(x => x.ServicePointId, StringComparer.OrdinalIgnoreCase);

        var listItems = new List<PharmacyQueueWorklistItemResponse>();

        foreach (var entry in activeEntries)
        {
            var spName = "";
            if (servicePoints.TryGetValue(entry.ServicePointId, out var sp))
            {
                spName = sp.DisplayName;
            }
            else
            {
                var loaded = _servicePointRepo.Get(entry.ServicePointId);
                if (loaded.HasValue)
                    spName = loaded.Value.DisplayName;
            }

            var mappings = _mappingRepo.ListByQueue(entry.AntrianId, entry.NoUrut);
            var pasienHadir = mappings.Any(m => m.PasienHadir);
            var pasienHadirAt = mappings
                .Where(m => m.PasienHadir)
                .Select(m => m.PasienHadirAt)
                .DefaultIfEmpty(ApotekDate.Empty)
                .Max();

            var demandIds = mappings
                .Select(m => m.DemandId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var patientName = "";
            var medicalRecordNo = "";
            var registrationId = "";
            var dokterName = "";
            var poliName = "";
            var penjamin = "";
            var isCito = false;
            var isTelaahApproved = false;

            if (_resepRepo is not null && demandIds.Count > 0)
            {
                foreach (var demandId in demandIds)
                {
                    var resepOpt = _resepRepo.LoadEntity(ResepModel.Key(demandId));
                    if (resepOpt.HasValue)
                    {
                        var resep = resepOpt.Value;
                        if (string.IsNullOrEmpty(registrationId))
                            registrationId = resep.Register?.RegId ?? "";
                        if (string.IsNullOrEmpty(patientName))
                            patientName = resep.Register?.PasienName ?? "";
                        if (string.IsNullOrEmpty(medicalRecordNo))
                            medicalRecordNo = resep.Register?.PasienId ?? "";
                        if (string.IsNullOrEmpty(dokterName))
                            dokterName = resep.Dokter?.DokterName ?? "";
                        if (string.IsNullOrEmpty(poliName))
                            poliName = resep.Layanan?.LayananName ?? "";
                        if (!isCito && resep.Urgenitas is not null)
                        {
                            isCito = resep.Urgenitas.UrgenitasName.Contains("CITO", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(resep.Urgenitas.UrgenitasId, "CITO", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(resep.Urgenitas.UrgenitasId, "1", StringComparison.OrdinalIgnoreCase);
                        }

                        if (string.IsNullOrEmpty(penjamin))
                            penjamin = resep.TipeBrg?.TipeBrgName ?? "";

                        if (_telaahRepo is not null)
                        {
                            var telaahOpt = _telaahRepo.LoadByResep(demandId);
                            if (telaahOpt.HasValue && telaahOpt.Value.CanEstablishSalesOrder)
                            {
                                isTelaahApproved = true;
                            }
                        }
                    }
                }

                if (_regRepo is not null && !string.IsNullOrWhiteSpace(registrationId))
                {
                    var regOpt = _regRepo.LoadEntity(RegModel.Key(registrationId));
                    if (regOpt.HasValue)
                    {
                        var reg = regOpt.Value;
                        if (reg.TipeJaminan is not null && !string.IsNullOrWhiteSpace(reg.TipeJaminan.TipeJaminanName))
                            penjamin = reg.TipeJaminan.TipeJaminanName;
                        if (reg.Pasien is not null && !string.IsNullOrWhiteSpace(reg.Pasien.PasienId))
                            medicalRecordNo = reg.Pasien.PasienId;
                        if (reg.Pasien is not null && !string.IsNullOrWhiteSpace(reg.Pasien.PasienName) && string.IsNullOrWhiteSpace(patientName))
                            patientName = reg.Pasien.PasienName;
                    }
                }
            }

            var matches = MatchesFilter(request, entry, isTelaahApproved, pasienHadir);
            if (!matches)
                continue;

            listItems.Add(new PharmacyQueueWorklistItemResponse(
                AntrianId: entry.AntrianId,
                NoUrut: entry.NoUrut,
                QueueLabel: entry.QueueLabel,
                ServicePointId: entry.ServicePointId,
                ServicePointName: spName,
                ApotekPhase: entry.ApotekPhase,
                CallCountPenerimaan: entry.CallCountPenerimaan,
                LastCalledPenerimaanAt: entry.LastCalledPenerimaanAt,
                CallCountSerah: entry.CallCountSerah,
                LastCalledSerahAt: entry.LastCalledSerahAt,
                TakenAt: entry.TakenAt,
                ServedAt: entry.ServedAt,
                PreparedAt: entry.PreparedAt,
                DoneAt: entry.DoneAt,
                PasienHadir: pasienHadir,
                PasienHadirAt: pasienHadirAt,
                DemandIds: demandIds,
                PatientName: patientName,
                MedicalRecordNo: medicalRecordNo,
                RegistrationId: registrationId,
                DokterName: dokterName,
                PoliName: poliName,
                Penjamin: penjamin,
                IsCito: isCito,
                IsPaymentOverridden: entry.IsPaymentOverridden,
                PaymentOverrideReason: entry.PaymentOverrideReason));
        }

        var orderedItems = listItems
            .OrderByDescending(x => x.IsCito)
            .ThenByDescending(x => x.PasienHadir)
            .ThenBy(x => x.NoUrut)
            .ToList();

        var totalCount = activeEntries.Count;
        var waitingCount = activeEntries.Count(x => x.ApotekPhase == ApotekPhaseEnum.TAKEN);
        var preparedCount = activeEntries.Count(x => x.ApotekPhase == ApotekPhaseEnum.SIAP_SERAH);

        var response = new PharmacyQueueWorklistResponse(
            request.DepoFarmasiId,
            orderedItems,
            totalCount,
            waitingCount,
            preparedCount);

        return Task.FromResult(response);
    }

    private bool MatchesFilter(
        PharmacyQueueWorklistQry request,
        ApotekQueueEntryModel entry,
        bool isTelaahApproved,
        bool pasienHadir)
    {
        if (request.Phase.HasValue)
            return entry.ApotekPhase == request.Phase.Value;

        if (string.IsNullOrWhiteSpace(request.FilterTab))
            return true;

        var tab = request.FilterTab.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");

        return tab switch
        {
            "menunggu" or "waiting" or "taken" =>
                entry.ApotekPhase == ApotekPhaseEnum.TAKEN,

            "menunggu telaah" or "menunggutelaah" or "telaah" =>
                entry.ApotekPhase == ApotekPhaseEnum.PENERIMAAN && !isTelaahApproved,

            "dilewati" or "skipped" =>
                entry.ApotekPhase == ApotekPhaseEnum.DILEWATI,

            "siap penjualan" or "siappenjualan" or "penjualan" or "readyforsale" =>
                isTelaahApproved || (_telaahRepo is null && pasienHadir && entry.ApotekPhase == ApotekPhaseEnum.PENERIMAAN),

            "siap serah" or "siapserah" or "serah" or "loket2" =>
                entry.ApotekPhase is ApotekPhaseEnum.SIAP_SERAH or ApotekPhaseEnum.SERAH_OBAT,

            "selesai" or "done" =>
                entry.ApotekPhase == ApotekPhaseEnum.SELESAI,

            "semua" or "all" =>
                true,

            _ => Enum.TryParse<ApotekPhaseEnum>(tab, true, out var phase) && entry.ApotekPhase == phase
        };
    }
}
