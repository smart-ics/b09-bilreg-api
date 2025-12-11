using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianRepo _antrianRepo;
    public BookingDeleteHandler(IBookingRepo bookingRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo)
    {
        _bookingRepo = bookingRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
    }

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {
        
        Guard.Against.NullOrWhiteSpace(request.BookingId);

        var booking = _bookingRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingId} not found")
            );

        // cek jadwal
        var dokter = PpaType.Key(booking.Dokter.PpaId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var hari = booking.TglBerobat.DayOfWeek;
        var jamMulai = booking.JamPraktek;
        var jadwal = listJadwal
             .Where(x => x.Hari == hari)
             .FirstOrDefault(x => x.JamMulai == jamMulai)
            ?? JadwalPraktekType.Default;

        //  ambil data antrian
        var listAntrian = _antrianRepo.ListData(booking.TglBerobat);
        var sequenceTag = AntrianModel.GenSequenceTag(booking.TglBerobat, jadwal);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag) ??
            new AntrianHeaderView("-", "", new DateOnly(3000,1,1), new TimeOnly(0,0), "");
        var antrian = _antrianRepo.LoadEntity(antrianView).Value;
        
        antrian.RemoveEntry(booking.NoAntrian);
        
        _bookingRepo.DeleteEntity(booking);
        _antrianRepo.SaveChanges(antrian);

        return Task.CompletedTask;
    }
}
