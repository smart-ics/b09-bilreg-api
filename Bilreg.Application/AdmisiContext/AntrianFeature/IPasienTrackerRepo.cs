using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IPasienTrackerRepo :
    ISaveChange<PasienTrackerModel>,
    ILoadEntity<PasienTrackerModel, IPasienTrackerKey>,
    IDeleteEntity<IPasienTrackerKey>
{
    IEnumerable<PasienTrackerView> ListData(Periode visitDate, DateOnly tglLahir);
}

public record PasienTrackerView(
    string PasienTrackerId,
    PersonType Person,
    DateOnly VisitDate);