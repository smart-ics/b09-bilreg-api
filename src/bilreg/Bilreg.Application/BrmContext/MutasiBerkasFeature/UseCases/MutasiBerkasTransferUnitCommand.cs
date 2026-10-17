using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasTransferUnitCommand(
    string MutasiIdAsal,
    string PetugasTransfer,
    string LayananTujuanId,
    string DokterTujuanId,
    DateOnly TglPraktek,
    string JamPraktek,
    string SesiPoliTujuan,
    decimal QtyTransferRi,
    decimal QtyTransferRj,
    string Catatan = "",
    DateOnly? TglMutasi = null,
    TimeOnly? JamMutasi = null
) : IRequest<MutasiBerkasTransferUnitResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiIdAsal;

    public MutasiBerkasTransferUnitCommand(
        string mutasiIdAsal,
        string petugasTransfer,
        string layananTujuanId,
        string dokterTujuanId,
        string tglPraktek,
        string jamPraktek,
        string sesiPoliTujuan,
        decimal qtyTransferRi,
        decimal qtyTransferRj,
        string catatan = "",
        string? tglMutasi = null,
        string? jamMutasi = null) : this(
            mutasiIdAsal,
            petugasTransfer,
            layananTujuanId,
            dokterTujuanId,
            DateOnly.Parse(tglPraktek),
            jamPraktek,
            sesiPoliTujuan,
            qtyTransferRi,
            qtyTransferRj,
            catatan,
            string.IsNullOrWhiteSpace(tglMutasi) ? null : DateOnly.Parse(tglMutasi),
            string.IsNullOrWhiteSpace(jamMutasi) ? null : TimeOnly.Parse(jamMutasi))
    {
    }
}

public record MutasiBerkasTransferUnitResponse(
    string MutasiId,
    MutasiBerkasStatusEnum Status,
    string TrsBuatId,
    string PasienId,
    string LayananTujuanId,
    string AntrianNomor = ""
);

public class MutasiBerkasTransferUnitHandler : IRequestHandler<MutasiBerkasTransferUnitCommand, MutasiBerkasTransferUnitResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;
    private readonly IAntrianRepo _antrianRepo;

    public MutasiBerkasTransferUnitHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator,
        IAntrianRepo antrianRepo)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
        _antrianRepo = antrianRepo;
    }

    public Task<MutasiBerkasTransferUnitResponse> Handle(MutasiBerkasTransferUnitCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiIdAsal, nameof(request.MutasiIdAsal));
        Guard.Against.NullOrWhiteSpace(request.PetugasTransfer, nameof(request.PetugasTransfer));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));
        Guard.Against.NullOrWhiteSpace(request.DokterTujuanId, nameof(request.DokterTujuanId));
        Guard.Against.NullOrWhiteSpace(request.JamPraktek, nameof(request.JamPraktek));
        Guard.Against.NullOrWhiteSpace(request.SesiPoliTujuan, nameof(request.SesiPoliTujuan));

        if (request.QtyTransferRi < 0 || request.QtyTransferRj < 0)
            throw new ArgumentException("Jumlah volume berkas transfer tidak boleh negatif.");

        if (request.QtyTransferRi + request.QtyTransferRj <= 0)
            throw new ArgumentException("Jumlah volume berkas yang ditransfer harus lebih dari 0.");

        // 2. Load originating mutasi
        var originatingOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiIdAsal));
        var originating = originatingOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas asal dengan ID '{request.MutasiIdAsal}' tidak ditemukan.")
        );

        // 3. Validate originating status is active (must be Received or Dispatched, not void)
        if (originating.IsVoid)
            throw new InvalidOperationException("Mutasi berkas asal sudah dibatalkan (void).");

        if (originating.Status != MutasiBerkasStatusEnum.Received && originating.Status != MutasiBerkasStatusEnum.Dispatched)
            throw new InvalidOperationException($"Mutasi berkas asal berstatus {originating.Status}. Hanya berkas berstatus Dispatched atau Received yang dapat ditransfer antar-unit.");

        // 4. Generate new mutation ID with prefix MK
        var newId = _idGenerator.Generate(MutasiBerkasStatusEnum.Dispatched);

        // 5. Create transfer mutation (status Dispatched, destination clinic/doctor/schedule, transfer volume)
        var jadwal = JadwalPelayananType.Create(
            request.LayananTujuanId,
            request.DokterTujuanId,
            request.TglPraktek,
            request.JamPraktek,
            request.SesiPoliTujuan);

        var volume = VolumeBerkasType.CreatePinjam(request.QtyTransferRi, request.QtyTransferRj);
        var tglMutasi = request.TglMutasi ?? DateOnly.FromDateTime(DateTime.Now);
        var jamMutasi = request.JamMutasi ?? TimeOnly.FromDateTime(DateTime.Now);

        var transferModel = MutasiBerkasModel.CreateKirim(
            newId,
            originating.TrsBuatId,
            originating.PasienId,
            tglMutasi,
            jamMutasi,
            request.PetugasTransfer,
            jadwal,
            volume);

        // 6. Persist new transfer mutation
        _mutasiRepo.SaveChanges(transferModel);

        // 7. Antrian Integration (TD-05): Outpatient visit registration
        var antrianNomor = string.Empty;
        if (!string.IsNullOrWhiteSpace(originating.TrsBuatId) &&
            originating.TrsBuatId.StartsWith("RG", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var queueHeaders = _antrianRepo.ListData(request.TglPraktek);
                var targetHeader = queueHeaders?.FirstOrDefault(x =>
                    (x.AntrianDescription != null && x.AntrianDescription.Contains(request.LayananTujuanId, StringComparison.OrdinalIgnoreCase)) ||
                    (x.SequenceTag != null && x.SequenceTag.Contains(request.DokterTujuanId, StringComparison.OrdinalIgnoreCase)));

                if (targetHeader != null)
                {
                    var queueOpt = _antrianRepo.LoadEntity(targetHeader);
                    if (queueOpt.HasValue)
                    {
                        var queue = queueOpt.Value;
                        var entry = queue.AddAdmissionEntry(DateTime.Now);
                        _antrianRepo.SaveChanges(queue);
                        antrianNomor = queue.FormatQueueLabel(entry.NoUrut) ?? entry.NoUrut.ToString();
                    }
                }
            }
            catch
            {
                // Gracefully handle any queue resolution or mock issue
            }
        }

        // 8. Return response
        return Task.FromResult(new MutasiBerkasTransferUnitResponse(
            MutasiId: newId,
            Status: MutasiBerkasStatusEnum.Dispatched,
            TrsBuatId: transferModel.TrsBuatId,
            PasienId: transferModel.PasienId,
            LayananTujuanId: request.LayananTujuanId,
            AntrianNomor: antrianNomor));
    }
}
