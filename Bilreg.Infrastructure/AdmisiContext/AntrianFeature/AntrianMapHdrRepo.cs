using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions.Equivalency.Steps;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianMapHdrRepo : IAntrianMapHdrRepo
{
    private readonly IAntrianMapHdrDal _hdrDal;
    private readonly IAntrianMapDal _mapDal;
    private readonly IJadwalPraktekRepo _jadwalRepo;

    public AntrianMapHdrRepo(IAntrianMapHdrDal hdrDal,
        IAntrianMapDal mapDal,
        IJadwalPraktekRepo jadwalRepo)
    {
        _hdrDal = hdrDal;
        _mapDal = mapDal;
        _jadwalRepo = jadwalRepo;
    }
    public void SaveChanges(AntrianMapHdrModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _hdrDal.Update(AntrianMapHdrDto.FromModel(model)),
                onNone: () => _hdrDal.Insert(AntrianMapHdrDto.FromModel(model))
            );
        var listDtlDto = model.ListMap.Select(x => AntrianMapDto.FromModel(x));

        _mapDal.Delete(model);
        listDtlDto.ForEach(x => _mapDal.Insert(x));
    }

    public MayBe<AntrianMapHdrModel> LoadEntity(IAntrianMapHdrKey key)
    {
        var hdr = _hdrDal.GetData(key);
        var listDtl = _mapDal.ListData(key)?.ToList() ?? [];
        var model = hdr?.ToModel(listDtl);
        return MayBe.From(model!);
    }

    public IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglBerobat)
    {
        var listAnt = _hdrDal.ListData(lynKey, ppaKey, tglBerobat)?.ToList() ?? [];
        var result = listAnt.Select(x => x.ToView())?.ToList() ?? [];
        
        return result;
    }

    public void Migrasi(DateTime date)
    {
        var listDtl = _mapDal.ListData(date)?.ToList() ?? [];
        var listTemp = listDtl.Select(x => 
            new AntrianMapDtlDto(
                x.fs_kd_dokter, x.fs_kd_layanan, 
                x.fd_tgl_jadwal, x.fs_jam_jadwal,
                x.fs_nm_dokter, x.fs_nm_layanan))
            .Distinct()
            .ToList();

        foreach (var item in listTemp)
        {
            var lynKey = LayananType.Key(item.LayananId);
            var ppaKey = PpaType.Key(item.DokterId);
            var tglSch = item.TglJadwal.ToDate("yyyy-MM-dd");
            var tglJadwal = DateOnly.FromDateTime(tglSch);
            var dataHdr = _hdrDal.ListData(lynKey, ppaKey, tglJadwal)?.ToList() ?? [];
            var hdrTemp = dataHdr.Where(x => x.fs_jam_praktek == item.JamJadwal)?.ToList() ?? [];
            var schs = _jadwalRepo.ListData(ppaKey)?.ToList() ?? [];
            var schThis = schs.Where(x => x.Layanan.LayananId == item.LayananId)
                .Where(x => x.Hari == tglSch.DayOfWeek)
                .FirstOrDefault(x => x.JamMulai.ToString("HH:mm") ==  item.JamJadwal)
                ?? JadwalPraktekType.Default;

            if (hdrTemp.Count == 0 && schThis.JadwalPraktekId != "-")
            {
                var hdr = new AntrianMapHdrDto(
                    schThis.JadwalPraktekId, item.DokterId, item.LayananId, item.TglJadwal.ToDate(),
                    item.JamJadwal, item.JamJadwal, item.DokterName, item.LayananName);

                _hdrDal.Insert(hdr);
            }

        }

    }
}

public record AntrianMapDtlDto(
    string DokterId, string LayananId,
    string TglJadwal, string JamJadwal,
    string DokterName, string LayananName);