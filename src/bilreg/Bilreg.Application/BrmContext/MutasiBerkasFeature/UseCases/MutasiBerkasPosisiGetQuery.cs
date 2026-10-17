using Ardalis.GuardClauses;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record MutasiBerkasPosisiGetQuery(string MrId) : IRequest<MutasiBerkasPosisiResponse>;

public record MutasiBerkasPosisiResponse(
    string MutasiId,
    string TrsBuatId,
    string MrId,
    string LayananId,
    string DokterId,
    string PetugasId,
    int Status,
    string StatusDescription,
    decimal QtyPinjamRi,
    decimal QtyKembaliRi,
    decimal QtyPinjamRj,
    decimal QtyKembaliRj,
    decimal TotalPinjam,
    decimal TotalKembali,
    decimal SisaPinjam,
    DateOnly TglMutasi,
    TimeOnly JamMutasi,
    bool IsFound = true
)
{
    public static MutasiBerkasPosisiResponse Empty(string mrId) => new(
        string.Empty,
        string.Empty,
        mrId,
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        "Tidak Ditemukan",
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        new DateOnly(3000, 1, 1),
        new TimeOnly(0, 0, 0),
        false
    );
}

public class MutasiBerkasPosisiGetHandler : IRequestHandler<MutasiBerkasPosisiGetQuery, MutasiBerkasPosisiResponse>
{
    private readonly IMutasiBerkasRepo _mutasiRepo;

    public MutasiBerkasPosisiGetHandler(IMutasiBerkasRepo mutasiRepo)
    {
        _mutasiRepo = mutasiRepo;
    }

    public Task<MutasiBerkasPosisiResponse> Handle(MutasiBerkasPosisiGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MrId, nameof(request.MrId));

        var maybeModel = _mutasiRepo.GetLatestActiveByMr(request.MrId);
        if (!maybeModel.HasValue)
            throw new KeyNotFoundException($"Mutasi berkas aktif untuk No RM '{request.MrId}' tidak ditemukan.");

        var model = maybeModel.Value;
        var response = new MutasiBerkasPosisiResponse(
            model.MutasiBerkasId,
            model.TrsBuatId,
            model.PasienId,
            model.JadwalPelayanan.LayananId,
            model.JadwalPelayanan.DokterId,
            model.PetugasId,
            (int)model.Status,
            model.Status.ToString(),
            model.Volume.QtyPinjamRi,
            model.Volume.QtyKembaliRi,
            model.Volume.QtyPinjamRj,
            model.Volume.QtyKembaliRj,
            model.Volume.TotalPinjam,
            model.Volume.TotalKembali,
            model.Volume.SisaPinjam,
            model.TglMutasi,
            model.JamMutasi,
            true
        );

        return Task.FromResult(response);
    }
}
