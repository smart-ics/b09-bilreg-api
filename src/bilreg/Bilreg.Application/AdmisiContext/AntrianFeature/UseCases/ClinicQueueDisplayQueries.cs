using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public sealed record ClinicQueueCallItem(
    string AntrianId,
    int NoUrut,
    string NomorAntrean,
    string NamaPasien,
    string PoliId,
    string PoliName,
    string DokterId,
    string DokterName,
    string RuangId,
    string RuangName,
    int CallCount,
    DateTime CalledAt)
{
    public string QueueCode => AntrianId;
    public string QueueLabel => NomorAntrean;
    public string PersonName => NamaPasien;
}

public sealed record ClinicQueueWaitingItem(
    string AntrianId,
    int NoUrut,
    string NomorAntrean,
    string NamaPasien,
    string PoliId,
    string PoliName,
    string DokterId,
    string DokterName,
    DateTime CreatedAt,
    bool Priority = false)
{
    public string QueueCode => AntrianId;
    public string QueueLabel => NomorAntrean;
    public string PersonName => NamaPasien;
}

public sealed record ClinicDisplaySnapshotResponse(
    IReadOnlyList<ClinicQueueCallItem> ActiveCalls,
    IReadOnlyList<ClinicQueueWaitingItem> WaitingItems);

public record ClinicQueueGetDisplaySnapshotQuery(
    string? DisplayId = null,
    IEnumerable<string>? PoliIds = null,
    string? Tanggal = null) : IRequest<ClinicDisplaySnapshotResponse>;

public sealed class ClinicQueueGetDisplaySnapshotHandler
    : IRequestHandler<ClinicQueueGetDisplaySnapshotQuery, ClinicDisplaySnapshotResponse>
{
    private readonly IClinicQueueOperationDal _clinicQueueDal;
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITglJamProvider _clock;
    private readonly ILayananRepo? _layananRepo;
    private readonly IPpaRepo? _ppaRepo;
    private readonly IRuangRepo? _ruangRepo;

    public ClinicQueueGetDisplaySnapshotHandler(
        IClinicQueueOperationDal clinicQueueDal,
        IAntrianRepo antrianRepo,
        ITglJamProvider clock,
        ILayananRepo? layananRepo = null,
        IPpaRepo? ppaRepo = null,
        IRuangRepo? ruangRepo = null)
    {
        _clinicQueueDal = clinicQueueDal;
        _antrianRepo = antrianRepo;
        _clock = clock;
        _layananRepo = layananRepo;
        _ppaRepo = ppaRepo;
        _ruangRepo = ruangRepo;
    }

    public Task<ClinicDisplaySnapshotResponse> Handle(
        ClinicQueueGetDisplaySnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var poliList = (request.PoliIds ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (poliList.Count == 0 && !string.IsNullOrWhiteSpace(request.DisplayId))
        {
            poliList = _clinicQueueDal.GetPolisByDisplayId(request.DisplayId.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (poliList.Count == 0)
        {
            return Task.FromResult(new ClinicDisplaySnapshotResponse(
                Array.Empty<ClinicQueueCallItem>(),
                Array.Empty<ClinicQueueWaitingItem>()));
        }

        var rawActiveCalls = _clinicQueueDal.GetActiveCallsByPoliIds(poliList);

        var activeCallItems = new List<ClinicQueueCallItem>();
        var waitingItems = new List<ClinicQueueWaitingItem>();
        var loadedQueues = new Dictionary<string, AntrianModel?>(StringComparer.OrdinalIgnoreCase);

        foreach (var call in rawActiveCalls)
        {
            if (call.CallStatus != ClinicQueueCallStatus.Active)
                continue;

            if (!loadedQueues.TryGetValue(call.AntrianId, out var queue))
            {
                queue = _antrianRepo.LoadEntity(AntrianModel.Key(call.AntrianId)).GetValueOrDefault();
                loadedQueues[call.AntrianId] = queue;
            }

            var entry = queue?.ListEntry.FirstOrDefault(e => e.NoUrut == call.NoUrut);
            var nomorAntrean = queue?.FormatQueueLabel(call.NoUrut) ?? call.NoUrut.ToString();
            var namaPasien = entry?.Visitor.PersonName ?? string.Empty;
            var poliName = ResolvePoliName(call.PoliId);
            var dokterName = ResolveDokterName(call.DokterId);
            var ruangName = ResolveRuangName(call.RuangId);

            activeCallItems.Add(new ClinicQueueCallItem(
                AntrianId: call.AntrianId,
                NoUrut: call.NoUrut,
                NomorAntrean: nomorAntrean,
                NamaPasien: namaPasien,
                PoliId: call.PoliId,
                PoliName: poliName,
                DokterId: call.DokterId,
                DokterName: dokterName,
                RuangId: call.RuangId,
                RuangName: ruangName,
                CallCount: call.CallCount,
                CalledAt: call.CalledAt));
        }

        var seenWaiting = new HashSet<(string AntrianId, int NoUrut)>();

        foreach (var call in rawActiveCalls)
        {
            if (!loadedQueues.TryGetValue(call.AntrianId, out var queue) || queue is null)
                continue;

            var poliName = ResolvePoliName(call.PoliId);
            var dokterName = ResolveDokterName(call.DokterId);

            var activeNoUrut = call.CallStatus == ClinicQueueCallStatus.Active ? call.NoUrut : -1;
            var waitEntries = queue.ListEntry
                .Where(e => e.AntrianStatus == AntrianStatusEnum.Waiting && e.NoUrut != activeNoUrut)
                .OrderByDescending(e => e.Priority)
                .ThenBy(e => e.NoUrut);

            foreach (var e in waitEntries)
            {
                if (!seenWaiting.Add((queue.AntrianId, e.NoUrut)))
                    continue;

                var waitNomor = queue.FormatQueueLabel(e.NoUrut) ?? e.NoUrut.ToString();
                waitingItems.Add(new ClinicQueueWaitingItem(
                    AntrianId: queue.AntrianId,
                    NoUrut: e.NoUrut,
                    NomorAntrean: waitNomor,
                    NamaPasien: e.Visitor.PersonName,
                    PoliId: call.PoliId,
                    PoliName: poliName,
                    DokterId: call.DokterId,
                    DokterName: dokterName,
                    CreatedAt: e.CreatedAt,
                    Priority: e.Priority));
            }
        }

        return Task.FromResult(new ClinicDisplaySnapshotResponse(
            activeCallItems,
            waitingItems));
    }

    private string ResolvePoliName(string poliId)
    {
        if (string.IsNullOrWhiteSpace(poliId)) return string.Empty;
        try
        {
            return _layananRepo?.LoadEntity(LayananType.Key(poliId)).Value?.LayananName ?? poliId;
        }
        catch
        {
            return poliId;
        }
    }

    private string ResolveDokterName(string dokterId)
    {
        if (string.IsNullOrWhiteSpace(dokterId)) return string.Empty;
        try
        {
            return _ppaRepo?.LoadEntity(PpaType.Key(dokterId)).Value?.PpaName ?? dokterId;
        }
        catch
        {
            return dokterId;
        }
    }

    private string ResolveRuangName(string? ruangId)
    {
        if (string.IsNullOrWhiteSpace(ruangId)) return string.Empty;
        try
        {
            return _ruangRepo?.LoadEntity(RuangType.Default with { RuangId = ruangId }).Value?.RuangName ?? ruangId;
        }
        catch
        {
            return ruangId;
        }
    }
}
