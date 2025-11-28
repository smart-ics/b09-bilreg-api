using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingPeriodeListQuery(string TglYmd) : IRequest<IEnumerable<BookingPeriodeListResponse>>;

public record BookingPeriodeListResponse(
    string BookingId,
    string BookingDate,
    string PersonName,
    RegReff Reg,
    string TglBerobat,
    string JamPraktek,
    LayananReff Layanan,
    PpaReff Dokter,
    int NoAntrian);

public class BookingListHandler : IRequestHandler<BookingPeriodeListQuery, IEnumerable<BookingPeriodeListResponse>>
{
    private readonly IBookingRepo _bookingRepo;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    public BookingListHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task<IEnumerable<BookingPeriodeListResponse>> Handle(BookingPeriodeListQuery request, CancellationToken cancellationToken)
    {
        Guard.IsNotEmpty(request.TglYmd);
        Guard.IsTrue(request.TglYmd.IsValidTgl(FORMAT_TGL_YMD));

        var tgl = request.TglYmd.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl);
        var listBooking = _bookingRepo.ListDataTglBerobat(periode)?.ToList() ?? [];

        var result = listBooking
            .Select(x => new BookingPeriodeListResponse(
                x.BookingId,
                x.BookingDate.ToString("yyyy-MM-dd"),
                x.Person.PersonName,
                x.Reg,
                x.TglBerobat.ToString("yyyy-MM-dd"),
                x.JamPraktek.ToString("HH:mm"),
                x.Layanan,
                x.Dokter,
                x.NoAntrian
                ));
        return Task.FromResult(result);
    }
}
