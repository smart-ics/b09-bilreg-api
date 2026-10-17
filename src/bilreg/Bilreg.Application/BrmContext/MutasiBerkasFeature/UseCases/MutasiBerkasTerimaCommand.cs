using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasTerimaCommand(
    string MutasiId,
    string PetugasTerima,
    DateOnly? TglTerima = null,
    TimeOnly? JamTerima = null,
    string TrsBuatId = "",
    string MrId = ""
) : IRequest<MutasiBerkasTerimaResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiId;

    public MutasiBerkasTerimaCommand(
        string mutasiId,
        string petugasTerima,
        string tglTerima,
        string jamTerima) : this(
            mutasiId,
            petugasTerima,
            string.IsNullOrWhiteSpace(tglTerima) ? null : DateOnly.Parse(tglTerima),
            string.IsNullOrWhiteSpace(jamTerima) ? null : TimeOnly.Parse(jamTerima))
    {
    }
}

public record MutasiBerkasTerimaResponse(
    string MutasiId,
    int Status = (int)MutasiBerkasStatusEnum.Received,
    string TrsBuatId = "",
    string MrId = "",
    string PetugasTerima = ""
);

public class MutasiBerkasTerimaHandler : IRequestHandler<MutasiBerkasTerimaCommand, MutasiBerkasTerimaResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;

    public MutasiBerkasTerimaHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
    }

    public Task<MutasiBerkasTerimaResponse> Handle(MutasiBerkasTerimaCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.PetugasTerima, nameof(request.PetugasTerima));

        if (string.IsNullOrWhiteSpace(request.MutasiId) &&
            string.IsNullOrWhiteSpace(request.MrId) &&
            string.IsNullOrWhiteSpace(request.TrsBuatId))
        {
            throw new ArgumentException("MutasiId, MrId, atau TrsBuatId harus diisi.");
        }

        // 2. Load aggregate
        MayBe<MutasiBerkasModel> modelOpt = MayBe<MutasiBerkasModel>.None;

        if (!string.IsNullOrWhiteSpace(request.MutasiId))
        {
            modelOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiId));
        }
        else if (!string.IsNullOrWhiteSpace(request.MrId))
        {
            modelOpt = _mutasiRepo.GetLatestActiveByMr(request.MrId);
        }
        else if (!string.IsNullOrWhiteSpace(request.TrsBuatId))
        {
            var list = _mutasiRepo.ListByTrsBuat(request.TrsBuatId);
            var candidate = list
                .Where(x => !x.IsVoid)
                .OrderByDescending(x => x.TglMutasi)
                .ThenByDescending(x => x.JamMutasi)
                .FirstOrDefault();
            modelOpt = candidate != null ? MayBe.From(candidate) : MayBe<MutasiBerkasModel>.None;
        }

        var model = modelOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas tidak ditemukan.")
        );

        // 3. Validate state machine transition preconditions
        if (model.IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (model.Status != MutasiBerkasStatusEnum.Dispatched)
            throw new InvalidOperationException($"Mutasi berkas tidak dapat diterima karena status saat ini {model.Status} (harus Dispatched).");

        // 4. Generate new ID with prefix MP
        var newId = _idGenerator.Generate(MutasiBerkasStatusEnum.Received);

        // 5. Apply receipt transition
        var tglTerima = request.TglTerima ?? DateOnly.FromDateTime(DateTime.Now);
        var jamTerima = request.JamTerima ?? TimeOnly.FromDateTime(DateTime.Now);

        model.SetId(newId);
        model.Terima(request.PetugasTerima, tglTerima, jamTerima);

        // 6. Persist aggregate
        _mutasiRepo.SaveChanges(model);

        // 7. Return response
        return Task.FromResult(new MutasiBerkasTerimaResponse(
            MutasiId: model.MutasiBerkasId,
            Status: (int)model.Status,
            TrsBuatId: model.TrsBuatId,
            MrId: model.PasienId,
            PetugasTerima: model.PetugasId));
    }
}
