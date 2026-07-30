using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IPasienRepo : 
    ISaveChange<PasienModel, IPasienKey>,
    ILoadEntity<PasienModel, IPasienKey>,
    IDeleteEntity<IPasienKey>,
    IListData<PasienPersonView, string>
{
    IEnumerable<PasienPersonView> SearchPasien(string keyword);
    IEnumerable<PasienPersonView> SearchPasienByPhone(string phone);
    MayBe<PasienPersonView> GetDataByNik(string nik);
}

public record PasienPersonView(
    string PasienId,
    bool IsActive,
    PersonInfoType Person)
{
    public static PasienPersonView Default()
        => new("-", false, PersonInfoType.Default);

    public bool IsEmpty => PasienId == "-";
}
