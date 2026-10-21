using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasPinjamInternalCommand(
    string MrId,
    string UnitPeminjamId,
    string PetugasPeminjam,
    string PetugasKirim,
    string Keperluan,
    DateOnly? TglPinjam = null,
    TimeOnly? JamPinjam = null
) : IRequest<MutasiBerkasPinjamInternalResponse>
{
    public MutasiBerkasPinjamInternalCommand(
        string mrId,
        string unitPeminjamId,
        string petugasPeminjam,
        string petugasKirim,
        string keperluan,
        string? tglPinjam,
        string? jamPinjam) : this(
            mrId,
            unitPeminjamId,
            petugasPeminjam,
            petugasKirim,
            keperluan,
            string.IsNullOrWhiteSpace(tglPinjam) ? null : DateOnly.Parse(tglPinjam),
            string.IsNullOrWhiteSpace(jamPinjam) ? null : TimeOnly.Parse(jamPinjam))
    {
    }
}

public record MutasiBerkasPinjamInternalResponse(
    string MutasiId,
    string TrsBuatId = "",
    string MrId = "",
    int Status = 1,
    decimal TotalPinjam = 1m,
    string UnitPeminjamId = "",
    string PetugasPeminjam = "",
    string PetugasKirim = "",
    string Keperluan = ""
);

public class MutasiBerkasPinjamInternalHandler : IRequestHandler<MutasiBerkasPinjamInternalCommand, MutasiBerkasPinjamInternalResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;

    public MutasiBerkasPinjamInternalHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
    }

    public Task<MutasiBerkasPinjamInternalResponse> Handle(MutasiBerkasPinjamInternalCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MrId, nameof(request.MrId));
        Guard.Against.NullOrWhiteSpace(request.UnitPeminjamId, nameof(request.UnitPeminjamId));
        Guard.Against.NullOrWhiteSpace(request.PetugasPeminjam, nameof(request.PetugasPeminjam));
        Guard.Against.NullOrWhiteSpace(request.PetugasKirim, nameof(request.PetugasKirim));
        Guard.Against.NullOrWhiteSpace(request.Keperluan, nameof(request.Keperluan));

        if (request.Keperluan.Trim().Length < 5)
            throw new ArgumentException("Keperluan peminjaman minimal 5 karakter.", nameof(request.Keperluan));

        // 2. Double-Dispatch Guard (Cross-Cutting Concern #2)
        var latestMutasi = _mutasiRepo.GetLatestActiveByMr(request.MrId);
        if (latestMutasi.HasValue)
        {
            var existing = latestMutasi.Value;
            if (!existing.IsVoid && (existing.Status == MutasiBerkasStatusEnum.Dispatched || existing.Status == MutasiBerkasStatusEnum.Received))
            {
                throw new InvalidOperationException("Berkas rekam medis sedang beredar aktif di unit lain dan belum kembali.");
            }
        }

        // 3. Generate Mutasi ID (prefix MK)
        var mutasiId = _idGenerator.Generate(MutasiBerkasStatusEnum.Dispatched);

        // 4. Generate transaction number & timestamps
        var tglMutasi = request.TglPinjam ?? DateOnly.FromDateTime(DateTime.Now);
        var jamMutasi = request.JamPinjam ?? TimeOnly.FromDateTime(DateTime.Now);
        var trsBuatId = $"PINJAM-{tglMutasi:yyyyMMdd}{jamMutasi:HHmmss}";

        // 5. Build domain aggregate
        var volume = VolumeBerkasType.CreatePinjam(qtyRi: 0m, qtyRj: 1m);
        var jadwal = new JadwalPelayananType(
            layananId: request.UnitPeminjamId,
            dokterId: request.PetugasPeminjam,
            tglPraktek: tglMutasi,
            jamPraktek: jamMutasi.ToString("HH:mm"),
            sesiPoliId: "-");

        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: mutasiId,
            trsBuatId: trsBuatId,
            pasienId: request.MrId,
            tglMutasi: tglMutasi,
            jamMutasi: jamMutasi,
            petugasId: request.PetugasKirim,
            jadwalPelayanan: jadwal,
            volume: volume);

        // 6. Persist aggregate (TA_TRS_MR_MUTASI and TC_MR8)
        _mutasiRepo.SaveChanges(model);

        // 7. Return response
        return Task.FromResult(new MutasiBerkasPinjamInternalResponse(
            MutasiId: model.MutasiBerkasId,
            TrsBuatId: model.TrsBuatId,
            MrId: model.PasienId,
            Status: (int)model.Status,
            TotalPinjam: model.Volume.TotalPinjam,
            UnitPeminjamId: request.UnitPeminjamId,
            PetugasPeminjam: request.PetugasPeminjam,
            PetugasKirim: request.PetugasKirim,
            Keperluan: request.Keperluan));
    }
}
