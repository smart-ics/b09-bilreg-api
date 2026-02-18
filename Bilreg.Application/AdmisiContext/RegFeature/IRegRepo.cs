using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegRepo :
    ISaveChange<RegModel>,
    IDelete<IRegKey>,
    ILoadEntity<RegModel, IRegKey>,
    IListData<RegView, Periode, ILayananKey>,
    IListData<RegSearchRegView, string>
{
}

public record RegView(
    string RegId, string TglMasuk,
    PasienReff Pasien,
    LayananReff Layanan,
    PpaReff Dokter);

public record RegSearchRegView(
    string RegId,
    string RegDate,
    string PasienId,
    string PasienName,
    string TipeJaminanName,
    string LayananName,
    string JenisReg,
    string JenisRegString);
