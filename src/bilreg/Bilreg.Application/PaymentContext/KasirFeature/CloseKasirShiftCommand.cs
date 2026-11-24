using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record CloseKasirShiftCommand(
    string ShiftId,
    decimal SaldoAkhirFisik,
    string KasirId) : IRequest<CloseKasirShiftResponse>, IKasirShiftKey;

public record CloseKasirShiftResponse(
    string ShiftId,
    string TglShift,
    string KodeShift,
    string KasirId,
    string Loket,
    string TglJamBuka,
    decimal SaldoAwal,
    string TglJamTutup,
    decimal SaldoAkhirFisik,
    decimal TotalTerimaSistem,
    decimal TotalKeluarSistem,
    decimal SaldoAkhirSistem,
    decimal SelisihKas,
    string StatusShift);

public class CloseKasirShiftHandler : IRequestHandler<CloseKasirShiftCommand, CloseKasirShiftResponse>
{
    private readonly IKasirShiftRepo _kasirShiftRepo;
    private readonly ITrsKasirRepo _trsKasirRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public CloseKasirShiftHandler(
        IKasirShiftRepo kasirShiftRepo,
        ITrsKasirRepo trsKasirRepo,
        ITglJamProvider tglJamProvider)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _trsKasirRepo = trsKasirRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<CloseKasirShiftResponse> Handle(CloseKasirShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.ShiftId, nameof(request.ShiftId));
        Guard.Against.NullOrWhiteSpace(request.KasirId, nameof(request.KasirId));
        Guard.Against.Negative(request.SaldoAkhirFisik, nameof(request.SaldoAkhirFisik));

        var shift = _kasirShiftRepo.GetKey(request.ShiftId)
            ?? throw new KeyNotFoundException($"Shift kasir {request.ShiftId} tidak ditemukan.");

        if (!shift.IsOpen)
        {
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} sudah berstatus TUTUP dan tidak dapat ditutup kembali.");
        }

        if (!string.Equals(shift.KasirId, request.KasirId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} bukan milik kasir {request.KasirId}.");
        }

        var trsList = _trsKasirRepo.ListByShift(shift.ShiftId) ?? Enumerable.Empty<TrsKasirModel>();
        var totalTerima = trsList
            .Where(t => t.JenisTrs == KasirConst.JenisTrsTerima && t.StatusTrs == KasirConst.StatusTrsBerhasil)
            .Sum(t => (t.Payments ?? Enumerable.Empty<TrsKasirPaymentType>())
                .Where(p => p.MetodeBayar.Equals(KasirConst.MetodeBayarTunai, StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.NilaiNetto));
        var totalKeluar = trsList
            .Where(t => t.JenisTrs == KasirConst.JenisTrsKeluar && t.StatusTrs == KasirConst.StatusTrsBerhasil)
            .Sum(t => (t.Payments ?? Enumerable.Empty<TrsKasirPaymentType>())
                .Where(p => p.MetodeBayar.Equals(KasirConst.MetodeBayarTunai, StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.NilaiNetto));

        shift.CloseShift(request.SaldoAkhirFisik, totalTerima, totalKeluar, _tglJamProvider.Now);
        _kasirShiftRepo.Save(shift);

        var response = new CloseKasirShiftResponse(
            ShiftId: shift.ShiftId,
            TglShift: shift.TglShift,
            KodeShift: shift.KodeShift,
            KasirId: shift.KasirId,
            Loket: shift.Loket,
            TglJamBuka: shift.TglJamBuka,
            SaldoAwal: shift.SaldoAwal,
            TglJamTutup: shift.TglJamTutup,
            SaldoAkhirFisik: shift.SaldoAkhirFisik,
            TotalTerimaSistem: shift.TotalTerimaSistem,
            TotalKeluarSistem: shift.TotalKeluarSistem,
            SaldoAkhirSistem: shift.SaldoAkhirSistem,
            SelisihKas: shift.SelisihKas,
            StatusShift: shift.StatusShift);

        return Task.FromResult(response);
    }
}
