using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;

#region Query DTOs

/// <summary>
/// Query pemuatan dokumen utuh penjualan farmasi header dan seluruh baris item berdasarkan ID transaksi (DU...).
/// </summary>
public record PenjualanGetByIdQry(string PenjualanId) : IRequest<PenjualanDetailResponse?>;

/// <summary>
/// Query riwayat transaksi penjualan farmasi aktif untuk nomor registrasi pasien tertentu.
/// </summary>
public record PenjualanListByRegQry(string RegId) : IRequest<IReadOnlyList<PenjualanSummaryResponse>>;

/// <summary>
/// Query worklist operasional tagihan penjualan farmasi pada tanggal bisnis tertentu.
/// </summary>
public record PenjualanWorklistQry(
    DateOnly BusinessDate,
    int? StatusFilter = null
) : IRequest<IReadOnlyList<PenjualanSummaryResponse>>;

#endregion

#region Query Handlers

/// <summary>
/// Handler pemuatan dokumen utuh penjualan farmasi header dan detail.
/// </summary>
public class PenjualanGetByIdHandler : IRequestHandler<PenjualanGetByIdQry, PenjualanDetailResponse?>
{
    private readonly IPenjualanRepo _penjualanRepo;

    public PenjualanGetByIdHandler(IPenjualanRepo penjualanRepo)
    {
        _penjualanRepo = penjualanRepo;
    }

    public Task<PenjualanDetailResponse?> Handle(PenjualanGetByIdQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PenjualanId, nameof(request.PenjualanId));

        var modelOpt = _penjualanRepo.LoadEntity(new PenjualanKey(request.PenjualanId));
        if (!modelOpt.HasValue)
            return Task.FromResult<PenjualanDetailResponse?>(null);

        var response = PenjualanDetailResponse.FromModel(modelOpt.Value);
        return Task.FromResult<PenjualanDetailResponse?>(response);
    }
}

/// <summary>
/// Handler pembacaan daftar penjualan aktif berdasarkan nomor registrasi pasien.
/// </summary>
public class PenjualanListByRegHandler : IRequestHandler<PenjualanListByRegQry, IReadOnlyList<PenjualanSummaryResponse>>
{
    private readonly IPenjualanRepo _penjualanRepo;

    public PenjualanListByRegHandler(IPenjualanRepo penjualanRepo)
    {
        _penjualanRepo = penjualanRepo;
    }

    public Task<IReadOnlyList<PenjualanSummaryResponse>> Handle(PenjualanListByRegQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var models = _penjualanRepo.ListByReg(request.RegId);
        var responses = models.Select(PenjualanSummaryResponse.FromModel).ToList();
        return Task.FromResult<IReadOnlyList<PenjualanSummaryResponse>>(responses);
    }
}

/// <summary>
/// Handler antrean worklist operasional penjualan apotek berdasarkan tanggal bisnis dan filter status.
/// </summary>
public class PenjualanWorklistHandler : IRequestHandler<PenjualanWorklistQry, IReadOnlyList<PenjualanSummaryResponse>>
{
    private readonly IPenjualanRepo _penjualanRepo;

    public PenjualanWorklistHandler(IPenjualanRepo penjualanRepo)
    {
        _penjualanRepo = penjualanRepo;
    }

    public Task<IReadOnlyList<PenjualanSummaryResponse>> Handle(PenjualanWorklistQry request, CancellationToken cancellationToken)
    {
        var models = _penjualanRepo.ListWorklist(request.BusinessDate);

        if (request.StatusFilter.HasValue)
        {
            models = models.Where(x => (int)x.Status == request.StatusFilter.Value).ToList();
        }

        var responses = models.Select(PenjualanSummaryResponse.FromModel).ToList();
        return Task.FromResult<IReadOnlyList<PenjualanSummaryResponse>>(responses);
    }
}

#endregion
