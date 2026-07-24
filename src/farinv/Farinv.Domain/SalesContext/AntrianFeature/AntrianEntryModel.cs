using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Farinv.Domain.SalesContext.AntrianFeature;

public class AntrianEntryModel
{
    private static readonly DateTime Sentinel = new(3000, 1, 1);

    #region CREATION
    public AntrianEntryModel(
        int noAntrian, AntrianStatusEnum status,
        DateTime takenAt, DateTime assignedAt, DateTime preparedAt,
        DateTime deliveredAt, DateTime cancelAt, DateTime servedAt,
        RegReff reg, string reffId, string reffDesc, string pasienTrackerId)
    {
        NoAntrian = noAntrian;
        AntrianStatus = status;
        TakenAt = takenAt;
        AssignedAt = assignedAt;
        PreparedAt = preparedAt;
        DeliveredAt = deliveredAt;
        CancelAt = cancelAt;
        ServedAt = servedAt;
        Reg = reg;
        ReffId = reffId;
        ReffDesc = reffDesc;
        PasienTrackerId = pasienTrackerId;
    }

    public static AntrianEntryModel Create(int noAntrian, RegReff reg, string reffId, string refDesc)
    {
        return new AntrianEntryModel(noAntrian, AntrianStatusEnum.Taken, DateTime.Now,
            Sentinel, Sentinel, Sentinel, Sentinel, Sentinel,
            reg, reffId, refDesc, "-");
    }

    public static AntrianEntryModel CreateIdentified(
        int noAntrian, RegReff reg, string pasienTrackerId, DateTime takenAt)
    {
        PharmacyTrackerIdentity.EnsureRealTrackerId(pasienTrackerId);
        Guard.Against.Null(reg, nameof(reg));

        return new AntrianEntryModel(noAntrian, AntrianStatusEnum.Taken, takenAt,
            Sentinel, Sentinel, Sentinel, Sentinel, Sentinel,
            reg, "-", "-", pasienTrackerId.Trim());
    }

    public static AntrianEntryModel Default =>
        new(-1, AntrianStatusEnum.Open, Sentinel, Sentinel,
            Sentinel, Sentinel, Sentinel, Sentinel,
            RegModel.Default.ToReff(), "-", "", "-");
    #endregion

    #region PROPERTIES
    public int NoAntrian { get; private set; }
    public AntrianStatusEnum AntrianStatus { get; private set; }
    public DateTime TakenAt { get; init; }
    public DateTime AssignedAt { get; private set; }
    public DateTime PreparedAt { get; private set; }
    public DateTime DeliveredAt { get; private set; }
    public DateTime CancelAt { get; private set; }
    public DateTime ServedAt { get; private set; }

    public RegReff Reg { get; private set; }
    public string ReffId { get; private set; }
    public string ReffDesc { get; private set; }
    public string PasienTrackerId { get; private set; }
    #endregion

    #region METHOD BEHAVIOUR
    internal void Assign(RegReff reg)
    {
        Guard.Against.Null(reg, nameof(reg));
        EnsureStatus(AntrianStatusEnum.Taken);

        AntrianStatus = AntrianStatusEnum.Assigned;
        AssignedAt = DateTime.Now;
        Reg = reg;
    }

    internal void Prepare()
    {
        EnsureStatus(AntrianStatusEnum.Assigned);
        PreparedAt = DateTime.Now;
        AntrianStatus = AntrianStatusEnum.Prepared;
    }

    internal void Deliver()
    {
        EnsureStatus(AntrianStatusEnum.Prepared);
        DeliveredAt = DateTime.Now;
        AntrianStatus = AntrianStatusEnum.Delivered;
    }

    internal void Cancel()
    {
        EnsureStatus(AntrianStatusEnum.Prepared);
        CancelAt = DateTime.Now;
        AntrianStatus = AntrianStatusEnum.Cancelled;
    }

    internal void ConfirmSale(string penjualanId, DateTime servedAt)
    {
        Guard.Against.NullOrWhiteSpace(penjualanId, nameof(penjualanId));
        PharmacyTrackerIdentity.EnsureRealTrackerId(PasienTrackerId);

        if (IsFinalState())
            throw new InvalidOperationException($"Status Antrian ({AntrianStatus}) cannot be change");

        if (AntrianStatus == AntrianStatusEnum.Prepared
            && ReffId == penjualanId
            && ServedAt == servedAt)
            return;

        ServedAt = servedAt;
        ReffId = penjualanId;
        ReffDesc = "PENJUALAN";

        if (AntrianStatus == AntrianStatusEnum.Taken)
            Assign(Reg);

        if (AntrianStatus == AntrianStatusEnum.Assigned)
            Prepare();
    }

    internal void SetReff(string reffId, string reffDesc)
    {
        ReffId = reffId;
        ReffDesc = reffDesc;
    }

    internal bool IsActiveForTracker(string pasienTrackerId)
        => PasienTrackerId == pasienTrackerId.Trim() && !IsFinalState();

    private void EnsureStatus(AntrianStatusEnum expected)
    {
        if (IsFinalState())
            throw new InvalidOperationException($"Status Antrian ({AntrianStatus}) cannot be change");

        if (AntrianStatus != expected)
            throw new InvalidOperationException($"Status Antrian must be {expected}");
    }

    internal bool IsFinalState() =>
        AntrianStatus == AntrianStatusEnum.Delivered || AntrianStatus == AntrianStatusEnum.Cancelled;
    #endregion
}
