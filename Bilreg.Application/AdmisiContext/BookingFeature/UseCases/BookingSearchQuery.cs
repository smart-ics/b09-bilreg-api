using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingSearchQuery(string TglBerobat, string Keyword) : IRequest<IEnumerable<BookingSearchResponse>>;

public record BookingSearchResponse(string BookingId, string BookingDate,
    RegReff Reg, LayananReff Layanan, PpaReff Dokter, string TglBerobat, 
    string JamPraktek, int NoAntrian, ExtAppReffType ExtAppRef);

public class BookingSearchHandler : IRequestHandler<BookingSearchQuery, IEnumerable<BookingSearchResponse>>
{
    private readonly IBookingRepo _bookingRepo;

    public BookingSearchHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task<IEnumerable<BookingSearchResponse>> Handle(BookingSearchQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TglBerobat);
        Guard.Against.NullOrWhiteSpace(request.Keyword);
        Guard.Against.InvalidDateFormat(request.TglBerobat, nameof(request.TglBerobat));

        var tgl = request.TglBerobat.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl);
        var listBooking = _bookingRepo.ListDataExtApp(periode)?.ToList() ?? [];

        var listSearch = listBooking
            .Where(x =>
                x.Reg.PasienId == request.Keyword ||
                x.ExtAppReff.ReffId == request.Keyword ||
                x.Person.Contact.ContactDetail == request.Keyword ||
                x.CoverageInfo.NoRujukan == request.Keyword ||
                x.CoverageInfo.NoPeserta == request.Keyword ||
                x.ExtAppReff.CheckInQr.Equals(request.Keyword, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var result = listSearch.Select(x => new BookingSearchResponse(
            x.BookingId, x.BookingDate.ToString("yyyy-MM-dd"), x.Reg, x.Layanan, x.Dokter,
            x.TglBerobat.ToString("yyyy-MM-dd"), x.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
            x.NoAntrian, x.ExtAppReff));

        return Task.FromResult(result);
    }
}


// SearchBooking
//1. By PasienId
//2. By ReffId 
//3. By NoTelp 
//4. By Rujukan 
//5. By NoPeserta
//6. By CheckInQr
