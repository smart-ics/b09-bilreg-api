using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using MediatR;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

public record BrgGetDetailQuery(string BrgId) : IRequest<BrgGetDetailResponse>, IBrgKey;

public record BrgGetDetailResponse(
    string BrgId,
    string BrgName,
    bool IsAktif,
    string KetBarang,
    string GroupRekId,
    string GroupRekName,
    string GroupRekDkId,
    string GroupRekDkName,
    string PabrikId,
    string PabrikName,
    string SatuanKecilId,
    string SatuanKecilName,
    string SatuanBesarId,
    string SatuanBesarName,
    int Konversi
);

public class BrgGetDetailHandler : IRequestHandler<BrgGetDetailQuery, BrgGetDetailResponse>
{
    private readonly IBrgRepo _brgRepo;

    public BrgGetDetailHandler(IBrgRepo brgRepo)
    {
        _brgRepo = brgRepo;
    }

    public Task<BrgGetDetailResponse> Handle(BrgGetDetailQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));

        var brg = _brgRepo.LoadEntity(request)
            .GetValueOrThrow($"Barang with Id: {request.BrgId} not found.");

        var response = new BrgGetDetailResponse(
            brg.BrgId,
            brg.BrgName,
            brg.IsAktif,
            brg.KetBarang,
            brg.GroupRek.GroupRekId,
            brg.GroupRek.GroupRekName,
            brg.GroupRekDk.GroupRekDkId,
            brg.GroupRekDk.GroupRekDkName,
            brg.Pabrik.PabrikId,
            brg.Pabrik.PabrikName,
            brg.SatuanKecil.SatuanId,
            brg.SatuanKecil.SatuanName,
            brg.SatuanBesar.SatuanId,
            brg.SatuanBesar.SatuanName,
            brg.Konversi
        );

        return Task.FromResult(response);
    }
}
