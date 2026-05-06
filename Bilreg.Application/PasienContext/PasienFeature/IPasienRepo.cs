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
    MayBe<PasienPersonView> GetDataByNik(string nik);
}

public record PasienPersonView(
    string PasienId,
    PersonInfoType Person)
{
    public static PasienPersonView Empty()
        => new("-", PersonInfoType.Default);

    public bool IsEmpty => PasienId == "-";
}