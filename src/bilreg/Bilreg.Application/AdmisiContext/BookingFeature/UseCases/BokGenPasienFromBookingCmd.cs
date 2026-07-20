using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BokGenPasienFromBookingCmd(
    string BookingId, 
    bool IsForceCreate) 
    : IRequest<BokGenPasienFromBookingResponse>;

public record BokGenPasienFromBookingResponse(
    string GeneratedPasienId,
    bool IsDuplicated,
    bool IsCreated,
    BokGenPasienFromBookingResponsePerson BookingData,
    IEnumerable<BokGenPasienFromBookingResponsePerson> ListExisting);

public record BokGenPasienFromBookingResponsePerson(
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string Alamat);

public class BokGenPasienFromBookingHandler 
    : IRequestHandler<BokGenPasienFromBookingCmd, BokGenPasienFromBookingResponse>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IPasienFactory _pasienFactory;
    private readonly ITglJamProvider _tglJamProvider;

    public BokGenPasienFromBookingHandler(IBookingRepo bookingRepo, 
        IPasienRepo pasienRepo, 
        IPasienFactory pasienFactory,
        ITglJamProvider tglJamProvider)
    {
        _bookingRepo = bookingRepo;
        _pasienRepo = pasienRepo;
        _pasienFactory = pasienFactory;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BokGenPasienFromBookingResponse> Handle(BokGenPasienFromBookingCmd request, 
        CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        var booking = _bookingRepo.LoadEntity(BookingModel.Key(request.BookingId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingId} not found")
            );

        var (generatedPasien, listDUplicated) = request.IsForceCreate 
            ? CreatePasien(booking, occurredAt)
            : ListOrCreatePasien(booking, occurredAt);

        var generatedPasienResp = new BokGenPasienFromBookingResponsePerson(
            generatedPasien.PasienId,
            booking.Person.PersonName,
            booking.Person.TglLahir.ToString("yyyy-MM-dd"),
            booking.Person.Gender,
            booking.Person.Alamat.Alamat[0] ?? string.Empty);
        
        var listDuplicatedResp = listDUplicated.Select(x => new BokGenPasienFromBookingResponsePerson(
            x.PasienId,
            x.Person.PersonName,
            x.Person.TglLahir.ToString("yyyy-MM-dd"),
            x.Person.Gender,
            x.Person.Alamat.Alamat[0] ?? string.Empty));
        
        var result = new BokGenPasienFromBookingResponse(
            generatedPasien.PasienId,
            listDUplicated.Count > 0,
            generatedPasien.PasienId != "-",
            generatedPasienResp,
            listDuplicatedResp);
        
        return Task.FromResult(result);        
    }

    private (PasienModel, List<PasienPersonView>) ListOrCreatePasien(BookingModel booking, DateTime occurredAt)
    {
        var listTglLahir = _pasienRepo
            .ListData(booking.Person.TglLahir.ToString("yyyy-MM-dd"));
        
        var listSimilar = listTglLahir
            .Where(x => x.Person.IsSimilar(booking.Person))
            .ToList();
        
        if (listSimilar.Count > 0)
            return (PasienModel.Default, listSimilar);
        
        var pasien = _pasienFactory
            .CreateFromPerson(booking.Person,
                booking.Person.PersonName.Split(' ')[0], "-", 
                GolDarahType.Default, "-", occurredAt);
        
        _pasienRepo.SaveChanges(pasien);
        return (pasien, []);
    }

    private (PasienModel, List<PasienPersonView>) CreatePasien(BookingModel booking, DateTime occurredAt)
    {
        var pasien = _pasienFactory.CreateFromPerson(booking.Person,
                booking.Person.PersonName.Split(' ')[0], "-", 
                GolDarahType.Default, "-", occurredAt);
        _pasienRepo.SaveChanges(pasien);
        return (pasien, []);
    }
}
