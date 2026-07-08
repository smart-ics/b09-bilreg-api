using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature;

public interface IReservationRepo :
    ISaveChange<ReservationModel>,
    ILoadEntity<ReservationModel, IReservationKey>
{
    IEnumerable<ReservationModel> ListData(ReservationListFilter filter);
}

public record ReservationListFilter(
    ReservationStatusEnum? Status = null,
    DateTime? PlannedFrom = null,
    DateTime? PlannedTo = null);
