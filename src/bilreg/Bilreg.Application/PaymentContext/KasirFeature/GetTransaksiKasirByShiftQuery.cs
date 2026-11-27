using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetTransaksiKasirByShiftQuery(string ShiftId) : IRequest<IEnumerable<TransaksiKasirSummaryDto>>;

public record TransaksiKasirSummaryDto(
    string TrsKasirId,
    string TglTrs,
    string JamTrs,
    string TglJamTrs,
    string JenisTrs,
    string RegId,
    string PasienId,
    string PasienName,
    string OrderRef,
    string Keterangan,
    decimal TotalNilai,
    string StatusTrs,
    bool IsVoid);

public class GetTransaksiKasirByShiftHandler : IRequestHandler<GetTransaksiKasirByShiftQuery, IEnumerable<TransaksiKasirSummaryDto>>
{
    private readonly ITrsKasirRepo _trsKasirRepo;

    public GetTransaksiKasirByShiftHandler(ITrsKasirRepo trsKasirRepo)
    {
        _trsKasirRepo = trsKasirRepo;
    }

    public Task<IEnumerable<TransaksiKasirSummaryDto>> Handle(GetTransaksiKasirByShiftQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.ShiftId, nameof(request.ShiftId));

        var list = (_trsKasirRepo.ListByShift(request.ShiftId.Trim()) ?? Enumerable.Empty<TrsKasirModel>())
            .OrderByDescending(x => x.TglJamTrs)
            .ThenByDescending(x => x.TrsKasirId)
            .Select(model => new TransaksiKasirSummaryDto(
                TrsKasirId: model.TrsKasirId,
                TglTrs: model.TglTrs,
                JamTrs: model.JamTrs,
                TglJamTrs: model.TglJamTrsStr,
                JenisTrs: model.JenisTrs,
                RegId: model.RegId,
                PasienId: model.PasienId,
                PasienName: model.PasienName,
                OrderRef: model.OrderRef,
                Keterangan: model.Keterangan,
                TotalNilai: model.TotalNilai,
                StatusTrs: model.StatusTrs,
                IsVoid: model.IsVoid))
            .ToList();

        return Task.FromResult<IEnumerable<TransaksiKasirSummaryDto>>(list);
    }
}
