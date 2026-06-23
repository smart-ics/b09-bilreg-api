using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public class JadwalPraktekHarianRepo : IJadwalPraktekHarianRepo
{
    private readonly JadwalPraktekHarianDal _dal;

    public JadwalPraktekHarianRepo(IOptions<DatabaseOptions> opt)
    {
        _dal = new JadwalPraktekHarianDal(opt);
    }

    public void SaveChanges(JadwalPraktekHarianType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(JadwalPraktekHarianDto.FromModel(model)),
                onNone: () => _dal.Insert(JadwalPraktekHarianDto.FromModel(model)));
    }

    public MayBe<JadwalPraktekHarianType> LoadEntity(IJadwalPraktekHarianKey key)
    {
        var result = _dal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<JadwalPraktekHarianType> ListByDate(DateOnly tglPraktek)
        => (_dal.ListByDate(tglPraktek) ?? []).Select(x => x.ToModel());

    public IEnumerable<JadwalPraktekHarianType> ListByDateAndDokter(DateOnly tglPraktek, IPpaKey dokter)
        => (_dal.ListByDateAndDokter(tglPraktek, dokter) ?? []).Select(x => x.ToModel());
}
