using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegInapModel
{
    public readonly List<RegDokterType> _listDokter;
    public RegInapModel(List<RegDokterType> listDokter, 
        string regId, ProsedurMasukInapType prosedurMasukInap)
    {
        _listDokter = listDokter;
        RegId = regId;
        ProsedurMasukInap = prosedurMasukInap;
    }

    public string RegId { get; init; }
    public ProsedurMasukInapType ProsedurMasukInap { get; init; }
    public PpaReff Dpjp => _listDokter
        .Where(x => x.DokterRole == DokterRoleEnum.Dpjp)
        .FirstOrDefault(x => x.IsActive)?.Dokter??PpaType.Default.ToReff();
    public IEnumerable<RegDokterType> ListDokter => _listDokter;
}