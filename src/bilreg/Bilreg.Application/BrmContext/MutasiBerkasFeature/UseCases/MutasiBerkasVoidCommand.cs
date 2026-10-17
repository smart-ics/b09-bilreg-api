using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasVoidCommand(
    string MutasiId,
    string PetugasVoid,
    string AlasanVoid,
    DateOnly? TglVoid = null,
    TimeOnly? JamVoid = null
) : IRequest<MutasiBerkasVoidResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiId;

    public MutasiBerkasVoidCommand(
        string mutasiId,
        string petugasVoid,
        string alasanVoid,
        string tglVoid,
        string jamVoid) : this(
            mutasiId,
            petugasVoid,
            alasanVoid,
            string.IsNullOrWhiteSpace(tglVoid) ? null : DateOnly.Parse(tglVoid),
            string.IsNullOrWhiteSpace(jamVoid) ? null : TimeOnly.Parse(jamVoid))
    {
    }
}

public record MutasiBerkasVoidResponse(
    string MutasiId,
    int StatusSebelumnya,
    int StatusRollback,
    string TrsBuatId = "",
    string PasienId = ""
)
{
    public string MrId => PasienId;
}

public class MutasiBerkasVoidHandler : IRequestHandler<MutasiBerkasVoidCommand, MutasiBerkasVoidResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;

    public MutasiBerkasVoidHandler(IMutasiBerkasRepo mutasiRepo)
    {
        _mutasiRepo = mutasiRepo;
    }

    public Task<MutasiBerkasVoidResponse> Handle(MutasiBerkasVoidCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiId, nameof(request.MutasiId));
        Guard.Against.NullOrWhiteSpace(request.PetugasVoid, nameof(request.PetugasVoid));
        Guard.Against.NullOrWhiteSpace(request.AlasanVoid, nameof(request.AlasanVoid));

        // 2. Load aggregate
        var modelOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiId));
        var model = modelOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas dengan ID '{request.MutasiId}' tidak ditemukan.")
        );

        // 3. Verify not already void
        if (model.IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        // 4. Record previous status
        var statusSebelumnya = (int)model.Status;

        // 5. Apply void domain behavior
        var tglVoid = request.TglVoid ?? DateOnly.FromDateTime(DateTime.Now);
        var jamVoid = request.JamVoid ?? TimeOnly.FromDateTime(DateTime.Now);
        model.Void(request.PetugasVoid, tglVoid, jamVoid, request.AlasanVoid);

        // 6. Save voided model to database
        _mutasiRepo.SaveChanges(model);

        // 7. Status Rollback Evaluation (TD-02)
        var allMutations = _mutasiRepo.ListByTrsBuat(model.TrsBuatId) ?? [];
        var latestActive = allMutations
            .Where(x => !x.IsVoid && x.MutasiBerkasId != model.MutasiBerkasId)
            .OrderByDescending(x => x.TglMutasi)
            .ThenByDescending(x => x.JamMutasi)
            .FirstOrDefault();

        var statusRollback = latestActive != null
            ? (int)latestActive.Status
            : (int)MutasiBerkasStatusEnum.Permintaan;

        // 8. Return response
        return Task.FromResult(new MutasiBerkasVoidResponse(
            MutasiId: model.MutasiBerkasId,
            StatusSebelumnya: statusSebelumnya,
            StatusRollback: statusRollback,
            TrsBuatId: model.TrsBuatId,
            PasienId: model.PasienId
        ));
    }
}
