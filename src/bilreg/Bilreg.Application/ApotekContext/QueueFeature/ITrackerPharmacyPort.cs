using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.ApotekContext.QueueFeature;

public record TrackerPharmacyCommandResult(bool Applied, string CorrelationId);

public interface ITrackerPharmacyPort
{
    AntrianStatusEnum GetStatus(string antrianId, int noUrut);
    TrackerPharmacyCommandResult ServeOnce(string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at);
    TrackerPharmacyCommandResult DoneOnce(string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at);
    TrackerPharmacyCommandResult WithdrawFromWaiting(string antrianId, int noUrut, string reason, string userId, DateTime at);
}

public interface IStockPharmacyPort
{
    string ReserveToTemporaryUnit(string dispensingId, int itemNo, string brgId, decimal qty);
    string RemoveOnHandover(string dispensingId, int itemNo, string brgId, decimal qty);
    string ReturnOnNoShow(string dispensingId, int itemNo, string brgId, decimal qty);
}
