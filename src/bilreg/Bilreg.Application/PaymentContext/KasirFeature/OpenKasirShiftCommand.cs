using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record OpenKasirShiftCommand(
    string KodeShift,
    string KasirId,
    string Loket,
    decimal SaldoAwal) : IRequest<OpenKasirShiftResponse>;

public record OpenKasirShiftResponse(
    string ShiftId,
    string TglShift,
    string KodeShift,
    string KasirId,
    string Loket,
    string TglJamBuka,
    decimal SaldoAwal,
    string StatusShift);

public class OpenKasirShiftHandler : IRequestHandler<OpenKasirShiftCommand, OpenKasirShiftResponse>
{
    private readonly IKasirShiftRepo _kasirShiftRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OpenKasirShiftHandler(
        IKasirShiftRepo kasirShiftRepo,
        ITglJamProvider tglJamProvider)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OpenKasirShiftResponse> Handle(OpenKasirShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.KasirId, nameof(request.KasirId));
        Guard.Against.NullOrWhiteSpace(request.KodeShift, nameof(request.KodeShift));
        Guard.Against.Negative(request.SaldoAwal, nameof(request.SaldoAwal));

        if (request.KodeShift != "1" && request.KodeShift != "2" && request.KodeShift != "3")
            throw new BusinessValidationException("Kode shift harus bernilai '1', '2', atau '3'.");

        var activeShift = _kasirShiftRepo.GetActiveShift(request.KasirId);
        if (activeShift is not null && activeShift.IsOpen)
        {
            throw new BusinessValidationException(
                $"Kasir '{request.KasirId}' masih memiliki sesi shift aktif ({activeShift.ShiftId}) yang belum ditutup.");
        }

        var openTime = _tglJamProvider.Now;
        var shift = KasirShiftModel.OpenShift(
            kodeShift: request.KodeShift,
            kasirId: request.KasirId,
            loket: request.Loket,
            saldoAwal: request.SaldoAwal,
            openTime: openTime);

        _kasirShiftRepo.Save(shift);

        var response = new OpenKasirShiftResponse(
            ShiftId: shift.ShiftId,
            TglShift: shift.TglShift,
            KodeShift: shift.KodeShift,
            KasirId: shift.KasirId,
            Loket: shift.Loket,
            TglJamBuka: shift.TglJamBuka,
            SaldoAwal: shift.SaldoAwal,
            StatusShift: shift.StatusShift);

        return Task.FromResult(response);
    }
}
