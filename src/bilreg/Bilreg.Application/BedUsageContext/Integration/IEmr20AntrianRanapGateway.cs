namespace Bilreg.Application.BedUsageContext.Integration;

/// <summary>
/// BILREG outbound gateway for EMR 2.0 Inpatient Queue synchronization (architecture TD-01, TD-02).
/// Dispatches async notifications to EMR 2.0 upon bed allocation (check-in), rooming-in (rawat gabung),
/// and bed discharge/void.
/// </summary>
public interface IEmr20AntrianRanapGateway
{
    /// <summary>
    /// Notifies EMR 2.0 that a patient has checked in to an inpatient bed.
    /// Route: POST /api/Antrian/AddRegRanap?regID={regId}
    /// </summary>
    Task AddRegRanapAsync(string regId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies EMR 2.0 that a newborn baby has been placed into rooming-in (rawat gabung).
    /// Route: POST /api/Antrian/AddRanapGabung?regID={babyRegId}
    /// </summary>
    Task AddRanapGabungAsync(string babyRegId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies EMR 2.0 that an inpatient bed registration has been discharged or cancelled.
    /// Route: DELETE /api/Antrian/DischargeRegRanap?regID={regId}&isGabung={isGabung}
    /// </summary>
    Task DischargeRegRanapAsync(string regId, bool isGabung = false, CancellationToken cancellationToken = default);
}
