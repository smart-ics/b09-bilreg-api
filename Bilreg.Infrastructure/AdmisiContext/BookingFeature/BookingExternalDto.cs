using Bilreg.Domain.AdmisiContext.BookingFeature;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record BookingExternalDto(
    string BookingId, string ExtAppName,
    string ReffId, string CheckInQr)
{
    public static BookingExternalDto FromModel(string BookingId,ExtAppReffType model)
    {
        var result = new BookingExternalDto(BookingId, model.ExtAppName,
            model.ReffId, model.CheckInQr);
        return result;
    }
    
}
