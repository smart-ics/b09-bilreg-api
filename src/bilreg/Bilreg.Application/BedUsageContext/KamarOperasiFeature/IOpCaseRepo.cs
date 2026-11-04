using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public interface IOpCaseRepo :
    ISaveChange<OpCaseModel>,
    ILoadEntity<OpCaseModel, IOrderOpKey>,
    IDeleteEntity<IOrderOpKey>
{
    IEnumerable<OpCaseOrderView> ListActiveOpCase();
    IEnumerable<OpCaseOrderView> ListActiveOpCase(OpCaseAktifFilter filter);
}

public record OpCaseOrderView(
    string OrderOpId,
    PasienReff Pasien,
    string NamaOperasi,
    PpaReff Dokter,
    int EstimasiDurasi,
    UrgencyLevelEnum UrgencyLevel, 
    DateTime PreferedDate,
    OpCaseStateEnum OpCaseState,
    string DokterPengorderId = "",
    string DokterPengorderName = "",
    string DokterOperatorId = "",
    string DokterOperatorName = "",
    string ProsedurTambahan = "",
    int KlasifikasiOperasi = 1);
