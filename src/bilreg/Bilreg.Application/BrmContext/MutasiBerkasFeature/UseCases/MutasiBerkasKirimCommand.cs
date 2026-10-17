using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasKirimCommand(
    string TrsBuatId,
    string MrId,
    string DokterId,
    string LayananId,
    DateOnly TglPraktek,
    string JamPraktek,
    string SesiPoli,
    string PetugasKirim,
    decimal QtyPinjamRi,
    decimal QtyPinjamRj,
    DateOnly? TglMutasi = null,
    TimeOnly? JamMutasi = null
) : IRequest<MutasiBerkasKirimResponse>
{
    public MutasiBerkasKirimCommand(
        string trsBuatId,
        string mrId,
        string dokterId,
        string layananId,
        string tglPraktek,
        string jamPraktek,
        string sesiPoli,
        string petugasKirim,
        decimal qtyPinjamRi,
        decimal qtyPinjamRj) : this(
            trsBuatId,
            mrId,
            dokterId,
            layananId,
            DateOnly.Parse(tglPraktek),
            jamPraktek,
            sesiPoli,
            petugasKirim,
            qtyPinjamRi,
            qtyPinjamRj)
    {
    }
}

public record MutasiBerkasKirimResponse(
    string MutasiId,
    string TrsBuatId = "",
    string MrId = "",
    int Status = 1,
    decimal TotalPinjam = 0m
);

public class MutasiBerkasKirimHandler : IRequestHandler<MutasiBerkasKirimCommand, MutasiBerkasKirimResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;

    public MutasiBerkasKirimHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
    }

    public Task<MutasiBerkasKirimResponse> Handle(MutasiBerkasKirimCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.TrsBuatId, nameof(request.TrsBuatId));
        Guard.Against.NullOrWhiteSpace(request.MrId, nameof(request.MrId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.JamPraktek, nameof(request.JamPraktek));
        Guard.Against.NullOrWhiteSpace(request.SesiPoli, nameof(request.SesiPoli));
        Guard.Against.NullOrWhiteSpace(request.PetugasKirim, nameof(request.PetugasKirim));

        if (request.QtyPinjamRi < 0 || request.QtyPinjamRj < 0)
            throw new ArgumentException("Jumlah volume berkas pinjam tidak boleh negatif.");

        if (request.QtyPinjamRi + request.QtyPinjamRj <= 0)
            throw new ArgumentException("Jumlah volume berkas yang dipinjam harus lebih dari 0.");

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

        // 4. Build domain aggregate
        var volume = VolumeBerkasType.CreatePinjam(request.QtyPinjamRi, request.QtyPinjamRj);
        var jadwal = JadwalPelayananType.Create(
            request.LayananId,
            request.DokterId,
            request.TglPraktek,
            request.JamPraktek,
            request.SesiPoli);

        var tglMutasi = request.TglMutasi ?? DateOnly.FromDateTime(DateTime.Now);
        var jamMutasi = request.JamMutasi ?? TimeOnly.FromDateTime(DateTime.Now);

        var model = MutasiBerkasModel.CreateKirim(
            mutasiId,
            request.TrsBuatId,
            request.MrId,
            tglMutasi,
            jamMutasi,
            request.PetugasKirim,
            jadwal,
            volume);

        // 5. Persist aggregate (TA_TRS_MR_MUTASI and TC_MR8)
        _mutasiRepo.SaveChanges(model);

        // 6. Return response
        return Task.FromResult(new MutasiBerkasKirimResponse(
            MutasiId: model.MutasiBerkasId,
            TrsBuatId: model.TrsBuatId,
            MrId: model.PasienId,
            Status: (int)model.Status,
            TotalPinjam: model.Volume.TotalPinjam));
    }
}
