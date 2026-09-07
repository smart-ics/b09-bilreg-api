using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;
using System.Text.RegularExpressions;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapWithBookingResolver : INunaResolver<(AntrianMapModel, AntrianMapDetilModel), JadwalPraktekType, DateOnly, BookingModel, PasienModel>
{ }
public class AntrianMapWithBookingResolver : IAntrianMapWithBookingResolver
{
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;

    public AntrianMapWithBookingResolver(
        IAntrianMapRepo antrianMapRepo,
        IJadwalPraktekFeatureResolver featureResolver)
    {
        _antrianMapRepo = antrianMapRepo;
        _featureResolver = featureResolver;
    }

    public Result<(AntrianMapModel, AntrianMapDetilModel)> Resolve(JadwalPraktekType jadwal, DateOnly tgl, BookingModel booking, PasienModel pasien)
    {
        var antrianMap = _antrianMapRepo.Find(jadwal, tgl)
            .Match(
                onSome: x => x,
                onNone: () => _featureResolver.UseResolver
                    ? AntrianMapModel.CreateFromEffective(
                        JadwalPraktekEffectiveMapper.FromTemplate(jadwal, tgl) with
                        {
                            JadwalPraktekHarianId = booking.JadwalPraktekHarianId,
                            JadwalPraktekId = booking.JadwalPraktekId ?? jadwal.JadwalPraktekId
                        }, tgl)
                    : AntrianMapModel.CreateFromJadwal(jadwal, tgl)
            );

        if (!antrianMap.ListMap.Any())
        {
            var listDetilDb = _antrianMapRepo.ListDetil(jadwal, tgl)?.ToList() ?? [];
            antrianMap.AttachDetil(listDetilDb);
        }

        if (!antrianMap.ListMap.Any())
            antrianMap.SeedingMap();

        // 1. UMUM
        var emptyMap = antrianMap.ListMap
            .Where(x => x.Flag == "UMUM")
            .Where(x => x.IsFreeSlot())
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();

        // 2. STRICT TIME - regex HH:mm
        emptyMap ??= antrianMap.ListMap
            .Where(x => Regex.IsMatch(x.Flag, @"^\d{2}:\d{2}$")) // 08:00,09:00... 
            .Where(x => x.IsFreeSlot())
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();

        // 3. AUTO overflow
        emptyMap ??= antrianMap.ListMap
            .Where(x => x.Flag == "AUTO")
            .Where(x => x.IsFreeSlot())
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();


        AntrianMapDetilModel newDetil;
        if (emptyMap is null)
        {
            newDetil = antrianMap.AddAuto(booking);
            newDetil.SetPasien(booking.Person.PersonName, pasien.PasienId, booking.BookingId);
        }
        else
        {
            emptyMap.SetPasien(booking.Person.PersonName, pasien.PasienId, booking.BookingId);
            newDetil = emptyMap;
        }
        return Result<(AntrianMapModel, AntrianMapDetilModel)>.Success((antrianMap, newDetil));

    }
}
