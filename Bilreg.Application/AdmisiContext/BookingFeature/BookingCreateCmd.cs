using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record BookingCreateCmd(string PasienName, string TglLahir, string Gender,
    string LayananId, string DokterId, string Tgl) : IRequest<BookingCreateResponse>;

public record BookingCreateResponse (string BookingId);

public class BookingCreateHandler : IRequestHandler<BookingCreateCmd, BookingCreateResponse>
{
    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {

        throw new NotImplementedException();
    }
}