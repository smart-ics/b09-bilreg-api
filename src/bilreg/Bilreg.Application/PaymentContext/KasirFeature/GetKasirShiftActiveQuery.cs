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
    private readonly ITrsKasirRepo _trsKasirRepo;

    public GetKasirShiftActiveHandler(
        IKasirShiftRepo kasirShiftRepo,
        ITrsKasirRepo trsKasirRepo)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _trsKasirRepo = trsKasirRepo;
    }

    public Task<KasirShiftActiveResponse?> Handle(GetKasirShiftActiveQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.KasirId, nameof(request.KasirId));

        var shift = _kasirShiftRepo.GetActiveShift(request.KasirId);
        if (shift is null)
            return Task.FromResult<KasirShiftActiveResponse?>(null);

        var trsList = _trsKasirRepo.ListByShift(shift.ShiftId) ?? Enumerable.Empty<TrsKasirModel>();
        var totalTerima = trsList
            .Where(t => t.JenisTrs == KasirConst.JenisTrsTerima && t.StatusTrs == KasirConst.StatusTrsBerhasil)
            .Sum(t => t.TotalNilai);
        var totalKeluar = trsList
            .Where(t => t.JenisTrs == KasirConst.JenisTrsKeluar && t.StatusTrs == KasirConst.StatusTrsBerhasil)
            .Sum(t => t.TotalNilai);
        var saldoKasBerjalan = shift.SaldoAwal + totalTerima - totalKeluar;

        var response = new KasirShiftActiveResponse(
            ShiftId: shift.ShiftId,
            TglShift: shift.TglShift,
            KodeShift: shift.KodeShift,
            KasirId: shift.KasirId,
            Loket: shift.Loket,
            TglJamBuka: shift.TglJamBuka,
            SaldoAwal: shift.SaldoAwal,
            TotalTerimaBerjalan: totalTerima,
            TotalKeluarBerjalan: totalKeluar,
            SaldoKasBerjalan: saldoKasBerjalan,
            StatusShift: shift.StatusShift);

        return Task.FromResult<KasirShiftActiveResponse?>(response);
    }
}
