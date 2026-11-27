using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetKasirShiftActiveQuery(string KasirId) : IRequest<KasirShiftActiveResponse?>;

public record KasirShiftActiveResponse(
    string ShiftId,
    string TglShift,
    string KodeShift,
    string KasirId,
    string Loket,
    string TglJamBuka,
    decimal SaldoAwal,
    decimal TotalTerimaBerjalan,
    decimal TotalKeluarBerjalan,
    decimal SaldoKasBerjalan,
    string StatusShift);

public class GetKasirShiftActiveHandler : IRequestHandler<GetKasirShiftActiveQuery, KasirShiftActiveResponse?>
{
    private readonly IKasirShiftRepo _kasirShiftRepo;
    private readonly IKasirShiftTotalsDal _trsKasirDal;

    public GetKasirShiftActiveHandler(
        IKasirShiftRepo kasirShiftRepo,
        IKasirShiftTotalsDal trsKasirDal)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _trsKasirDal = trsKasirDal;
    }

    public async Task<KasirShiftActiveResponse?> Handle(GetKasirShiftActiveQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.KasirId, nameof(request.KasirId));

        var shift = _kasirShiftRepo.GetActiveShift(request.KasirId);
        if (shift is null)
            return null;

        var totals = await _trsKasirDal.GetShiftTotalsAsync(shift.ShiftId, cancellationToken);
        var saldoKasBerjalan = shift.SaldoAwal + totals.TotalTerima - totals.TotalKeluar;

        var response = new KasirShiftActiveResponse(
            ShiftId: shift.ShiftId,
            TglShift: shift.TglShift,
            KodeShift: shift.KodeShift,
            KasirId: shift.KasirId,
            Loket: shift.Loket,
            TglJamBuka: shift.TglJamBuka,
            SaldoAwal: shift.SaldoAwal,
            TotalTerimaBerjalan: totals.TotalTerima,
            TotalKeluarBerjalan: totals.TotalKeluar,
            SaldoKasBerjalan: saldoKasBerjalan,
            StatusShift: shift.StatusShift);

        return response;
    }
}
