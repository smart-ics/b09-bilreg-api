using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDokterListQuery(string TglYmd, string DokterId) : IRequest<IEnumerable<BookingDokterListResponse>>;

public record BookingDokterListResponse(
    string BookingId,
    string BookingDate,
    string PersonName,
    RegReff Reg,
    string TglBerobat,
    string JamPraktek,
    LayananReff Layanan,
    PpaReff Dokter,
    int NoAntrian);

public class BookingDokterListHandler : IRequestHandler<BookingDokterListQuery, IEnumerable<BookingDokterListResponse>>
{
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    private readonly IBookingRepo _bookingRepo;
    public BookingDokterListHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task<IEnumerable<BookingDokterListResponse>> Handle(BookingDokterListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(request.TglYmd);
        Guard.Against.InvalidDateFormat(request.TglYmd, nameof(request.TglYmd));

        var tgl = request.TglYmd.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl);
        var listBooking = _bookingRepo.ListDataTglBerobat(periode)?.ToList() ?? [];
        var listBookingDokter = listBooking
            .Where(x => x.Dokter.PpaId == request.DokterId)?
            .ToList() ?? [];
        var result = listBookingDokter
            .Select(x => new BookingDokterListResponse(
                x.BookingId, 
                x.BookingDate.ToString("yyyy-MM-dd"), 
                x.Person.PersonName, 
                x.Reg, 
                x.TglBerobat.ToString("yyyy-MM-dd"), 
                x.JamPraktek.ToString("HH:mm"), 
                x.Layanan, 
                x.Dokter, 
                x.NoAntrian));

        return Task.FromResult(result);

    }
}
