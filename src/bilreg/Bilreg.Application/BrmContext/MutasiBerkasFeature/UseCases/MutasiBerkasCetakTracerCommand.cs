using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasCetakTracerCommand(
    string MutasiId,
    string PetugasCetak,
    DateOnly? TglCetak = null,
    TimeOnly? JamCetak = null
) : IRequest<MutasiBerkasCetakTracerResponse>, IMutasiBerkasKey
{
    public string MutasiBerkasId => MutasiId;

    public MutasiBerkasCetakTracerCommand(
        string mutasiId,
        string petugasCetak,
        string tglCetak,
        string jamCetak) : this(
            mutasiId,
            petugasCetak,
            DateOnly.Parse(tglCetak),
            TimeOnly.Parse(jamCetak))
    {
    }
}

public record MutasiBerkasCetakTracerResponse(
    string MutasiId,
    int FrekuensiCetak = 0,
    string PetugasCetak = "",
    DateOnly? TglCetak = null,
    TimeOnly? JamCetak = null
);

public class MutasiBerkasCetakTracerHandler : IRequestHandler<MutasiBerkasCetakTracerCommand, MutasiBerkasCetakTracerResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;

    public MutasiBerkasCetakTracerHandler(IMutasiBerkasRepo mutasiRepo)
    {
        _mutasiRepo = mutasiRepo;
    }

    public Task<MutasiBerkasCetakTracerResponse> Handle(MutasiBerkasCetakTracerCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate request inputs
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiId, nameof(request.MutasiId));
        Guard.Against.NullOrWhiteSpace(request.PetugasCetak, nameof(request.PetugasCetak));

        // 2. Load aggregate
        var modelOpt = _mutasiRepo.LoadEntity(MutasiBerkasModel.Key(request.MutasiId));
        var model = modelOpt.Match(
            onSome: m => m,
            onNone: () => throw new KeyNotFoundException($"Mutasi berkas dengan ID '{request.MutasiId}' tidak ditemukan.")
        );

        // 3. Invoke domain behavior
        var tglCetak = request.TglCetak ?? DateOnly.FromDateTime(DateTime.Now);
        var jamCetak = request.JamCetak ?? TimeOnly.FromDateTime(DateTime.Now);
        model.CetakTracer(request.PetugasCetak, tglCetak, jamCetak);

        // 4. Save changes
        _mutasiRepo.SaveChanges(model);

        // 5. Return response
        return Task.FromResult(new MutasiBerkasCetakTracerResponse(
            MutasiId: model.MutasiBerkasId,
            FrekuensiCetak: model.CetakTracerInfo.FrekuensiCetak,
            PetugasCetak: model.CetakTracerInfo.PetugasCetak,
            TglCetak: model.CetakTracerInfo.TglCetak,
            JamCetak: model.CetakTracerInfo.JamCetak));
    }
}
