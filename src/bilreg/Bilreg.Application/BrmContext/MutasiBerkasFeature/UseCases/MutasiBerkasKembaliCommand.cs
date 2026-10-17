using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasKembaliCommand(
    string MutasiId,
    string PetugasKembali,
    decimal QtyKembaliRi,
    decimal QtyKembaliRj,
    DateOnly? TglKembali = null,
    TimeOnly? JamKembali = null
) : IRequest<MutasiBerkasKembaliResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiId;

    public MutasiBerkasKembaliCommand(
        string mutasiId,
        string petugasKembali,
        decimal qtyKembaliRi,
        decimal qtyKembaliRj,
        string tglKembali,
        string jamKembali) : this(
            mutasiId,
            petugasKembali,
            qtyKembaliRi,
            qtyKembaliRj,
            string.IsNullOrWhiteSpace(tglKembali) ? null : DateOnly.Parse(tglKembali),
            string.IsNullOrWhiteSpace(jamKembali) ? null : TimeOnly.Parse(jamKembali))
    {
    }
}

public record MutasiBerkasKembaliResponse(
    string MutasiId,
    int Status,
    VolumeBerkasType Volume,
    string TrsBuatId = "",
    string MrId = "",
    string PetugasKembali = ""
);

public class MutasiBerkasKembaliHandler : IRequestHandler<MutasiBerkasKembaliCommand, MutasiBerkasKembaliResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;
    private readonly IMutasiBerkasIdGenerator _idGenerator;

    public MutasiBerkasKembaliHandler(
        IMutasiBerkasRepo mutasiRepo,
        IMutasiBerkasIdGenerator idGenerator)
    {
        _mutasiRepo = mutasiRepo;
        _idGenerator = idGenerator;
    }

    public Task<MutasiBerkasKembaliResponse> Handle(MutasiBerkasKembaliCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiId, nameof(request.MutasiId));
        Guard.Against.NullOrWhiteSpace(request.PetugasKembali, nameof(request.PetugasKembali));

        if (request.QtyKembaliRi < 0 || request.QtyKembaliRj < 0)
            throw new ArgumentException("Jumlah volume berkas kembali tidak boleh negatif.");

        if (request.QtyKembaliRi + request.QtyKembaliRj <= 0)
            throw new ArgumentException("Jumlah volume berkas kembali harus lebih dari 0.");

        // 2. Load aggregate
        var modelOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiId));
        var model = modelOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas dengan ID '{request.MutasiId}' tidak ditemukan.")
        );

        // 3. Validate state machine transition preconditions
        if (model.IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (model.Status != MutasiBerkasStatusEnum.Dispatched && model.Status != MutasiBerkasStatusEnum.Received)
            throw new InvalidOperationException($"Mutasi berkas tidak dapat dikembalikan karena status saat ini {model.Status} (harus Dispatched atau Received).");

        if (request.QtyKembaliRi > model.Volume.QtyPinjamRi)
            throw new ArgumentException("Jumlah berkas rawat inap kembali melebihi jumlah pinjam.", nameof(request.QtyKembaliRi));

        if (request.QtyKembaliRj > model.Volume.QtyPinjamRj)
            throw new ArgumentException("Jumlah berkas rawat jalan kembali melebihi jumlah pinjam.", nameof(request.QtyKembaliRj));

        // 4. Generate new ID with prefix MM
        var newId = _idGenerator.Generate(MutasiBerkasStatusEnum.Returned);

        // 5. Apply return transition
        var tglKembali = request.TglKembali ?? DateOnly.FromDateTime(DateTime.Now);
        var jamKembali = request.JamKembali ?? TimeOnly.FromDateTime(DateTime.Now);

        model.SetId(newId);
        model.Kembali(request.PetugasKembali, tglKembali, jamKembali, request.QtyKembaliRi, request.QtyKembaliRj);

        // 6. Persist aggregate (atomically updates TA_TRS_MR_MUTASI and TC_MR8)
        _mutasiRepo.SaveChanges(model);

        // 7. Return response
        return Task.FromResult(new MutasiBerkasKembaliResponse(
            MutasiId: model.MutasiBerkasId,
            Status: (int)model.Status,
            Volume: model.Volume,
            TrsBuatId: model.TrsBuatId,
            MrId: model.PasienId,
            PetugasKembali: model.PetugasId));
    }
}
