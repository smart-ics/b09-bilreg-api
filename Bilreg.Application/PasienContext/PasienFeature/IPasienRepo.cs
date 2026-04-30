using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IPasienRepo : 
    ISaveChange<PasienModel, IPasienKey>,
    ILoadEntity<PasienModel, IPasienKey>,
    IDeleteEntity<IPasienKey>,
    IListData<PasienPersonView, string>
{
    IEnumerable<PasienPersonView> SearchPasien(string keyword);
}

public record PasienPersonView(
    string PasienId,
    PersonInfoType Person);
