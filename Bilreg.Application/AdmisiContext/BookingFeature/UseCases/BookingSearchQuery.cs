using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

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

        var byPasien = listBooking.Where(x => x.Reg.PasienId == request.Keyword) ?? [];
        var byQr = listBooking.Where(x => x.ExtAppReff.CheckInQr.ToLower() == request.Keyword.ToLower()) ?? [];
        var byReffId = listBooking.Where(x => x.ExtAppReff.ReffId == request.Keyword) ?? [];
        //var byTelp = listBooking.Where(x => x.)
        

        var listSearch = byQr.Concat(byReffId);
        var result = listSearch.Select(x => new BookingSearchResponse(
            x.BookingId, "3000-01-01", x.Reg, x.Layanan, x.Dokter,
            x.TglBerobat.ToString("yyyy-MM-dd"), x.JamPraktek.ToString("HH:mm"),
            x.NoAntrian, x.ExtAppReff));

        return Task.FromResult(result);
    }
}


// SearchBooking
//1. By PasienId
//2. By CheckInQr
//3. By ReffId 
//4. By NoTelp 
//5. By Rujukan 
//6. By NoPeserta 