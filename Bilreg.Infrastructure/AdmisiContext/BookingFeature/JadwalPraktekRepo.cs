using System.Data;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class JadwalPraktekRepo : IJadwalPraktekRepo
{
    private readonly JadwalPraktekDal _dal;
    public JadwalPraktekRepo(IOptions<DatabaseOptions> opt)
    {
        _dal = new JadwalPraktekDal(opt);
    }

    public void SaveChanges(JadwalPraktekType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(JadwalPraktekDto.FromModel(model)),
                onNone: () => _dal.Insert(JadwalPraktekDto.FromModel(model))
            );
    }
    
    public MayBe<JadwalPraktekType> LoadEntity(IJadwalPraktekKey key)
    {
        var result = _dal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public void DeleteEntity(IJadwalPraktekKey key)
        => _dal.Delete(key);

    public IEnumerable<JadwalPraktekType> ListData(IPpaKey filter)
    {
        var result = _dal.ListData(filter);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public IEnumerable<JadwalPraktekType> ListData(ILayananKey lyn)
    {
        var result = _dal.ListData(lyn);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public IEnumerable<JadwalPraktekType> ListData()
    {
        var result = _dal.ListData();
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public IEnumerable<JadwalPraktekType> ListData(ILayananDkKey lynDk)
    {
        var result = _dal.ListData(lynDk);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public IEnumerable<JadwalPraktekType> ListData(IGroupSpesialisKey grpSpesialis)
    {
        var result = _dal.ListData(grpSpesialis);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }
}
