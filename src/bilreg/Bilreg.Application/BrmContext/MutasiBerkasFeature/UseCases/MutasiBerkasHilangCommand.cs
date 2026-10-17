using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasHilangCommand(
    string MutasiId,
    string Petugas,
    DateOnly? TglMutasi = null,
    TimeOnly? JamMutasi = null
) : IRequest<MutasiBerkasHilangResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiId;

    public MutasiBerkasHilangCommand(
        string mutasiId,
        string petugas,
        string tglMutasi,
        string jamMutasi) : this(
            mutasiId,
            petugas,
            string.IsNullOrWhiteSpace(tglMutasi) ? null : DateOnly.Parse(tglMutasi),
            string.IsNullOrWhiteSpace(jamMutasi) ? null : TimeOnly.Parse(jamMutasi))
    {
    }
}

public record MutasiBerkasHilangResponse(
    string MutasiId,
    int Status = (int)MutasiBerkasStatusEnum.Missing,
    string TrsBuatId = "",
    string MrId = "",
    string Petugas = ""
);

public class MutasiBerkasHilangHandler : IRequestHandler<MutasiBerkasHilangCommand, MutasiBerkasHilangResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;

    public MutasiBerkasHilangHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
    }

    public Task<MutasiBerkasHilangResponse> Handle(MutasiBerkasHilangCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiId, nameof(request.MutasiId));
        Guard.Against.NullOrWhiteSpace(request.Petugas, nameof(request.Petugas));

        // 2. Load aggregate
        var modelOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiId));
        var model = modelOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas dengan ID '{request.MutasiId}' tidak ditemukan.")
        );

        // 3. Validate state machine transition preconditions
        if (model.IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (model.Status == MutasiBerkasStatusEnum.Returned)
            throw new InvalidOperationException("Berkas sudah dikembalikan ke ruang rekam medis, tidak dapat ditandai hilang.");

        if (model.Status == MutasiBerkasStatusEnum.Missing)
            throw new InvalidOperationException("Berkas sudah berstatus hilang.");

        // 4. Generate new ID with prefix MH
        var newId = _idGenerator.Generate(MutasiBerkasStatusEnum.Missing);

        // 5. Apply missing transition
        var tglMutasi = request.TglMutasi ?? DateOnly.FromDateTime(DateTime.Now);
        var jamMutasi = request.JamMutasi ?? TimeOnly.FromDateTime(DateTime.Now);

        model.SetId(newId);
        model.TandaiHilang(request.Petugas, tglMutasi, jamMutasi);

        // 6. Persist aggregate
        _mutasiRepo.SaveChanges(model);

        // 7. Return response
        return Task.FromResult(new MutasiBerkasHilangResponse(
            MutasiId: model.MutasiBerkasId,
            Status: (int)model.Status,
            TrsBuatId: model.TrsBuatId,
            MrId: model.PasienId,
            Petugas: model.PetugasId));
    }
}
