using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapWithBookingResolver : INunaResolver<(AntrianMapModel, AntrianMapDetilModel), JadwalPraktekType, DateOnly, BookingModel>
{ }
public class AntrianMapWithBookingResolver : IAntrianMapWithBookingResolver
{
    private readonly IAntrianMapRepo _antrianMapRepo;

    public AntrianMapWithBookingResolver(IAntrianMapRepo antrianMapRepo)
    {
        _antrianMapRepo = antrianMapRepo;
    }

    public Result<(AntrianMapModel, AntrianMapDetilModel)> Resolve(JadwalPraktekType jadwal, DateOnly tgl, BookingModel booking)
    {
        var antrianMap = _antrianMapRepo.Find(jadwal, tgl)
            .Match(
                onSome: x => x,
                onNone: () => AntrianMapModel.CreateFromJadwal(jadwal, tgl)
            );

        if (!antrianMap.ListMap.Any())
        {
            var listDetilDb = _antrianMapRepo.ListDetil(jadwal, tgl)?.ToList() ?? [];
            antrianMap.AttachDetil(listDetilDb);
        }

        if (!antrianMap.ListMap.Any())
            antrianMap.SeedingMap();

        var emptyMap = antrianMap.ListMap
            .Where(x => x.Flag == "UMUM")
            .Where(x => x.ReffId.Trim() == "")
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();
        emptyMap = emptyMap ??
                   antrianMap.ListMap
                       .Where(x => x.Flag == "AUTO")
                       .Where(x => x.ReffId.Trim() == "")
                       .OrderBy(x => x.NoUrut)
                       .FirstOrDefault();

        AntrianMapDetilModel newDetil;
        if (emptyMap is null)
        {
            newDetil = antrianMap.AddAuto(booking);
        }
        else
        {
            emptyMap.SetPasien(booking.Person.PersonName, booking.PasienId, booking.BookingId);
            newDetil = emptyMap;
        }
        return Result<(AntrianMapModel, AntrianMapDetilModel)>.Success((antrianMap, newDetil));

    }
}
