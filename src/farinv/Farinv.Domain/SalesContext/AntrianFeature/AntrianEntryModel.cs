using Ardalis.GuardClauses;

namespace Farinv.Domain.SalesContext.AntrianFeature;

public class AntrianEntryModel
{
    #region CREATION
    public AntrianEntryModel(
        int noAntrian, AntrianStatusEnum status,
        DateTime takenAt, DateTime assignedAt, DateTime preparedAt,
        DateTime deliveredAt, DateTime cancelAt,
        RegReff reg, string reffId, string reffDesc)
    {
        NoAntrian = noAntrian;
        AntrianStatus = status;
        TakenAt = takenAt;
        AssignedAt = assignedAt;
        PreparedAt = preparedAt;
        DeliveredAt = deliveredAt;
        CancelAt = cancelAt;
        Reg = reg;
        ReffId = reffId;
        ReffDesc = reffDesc;
    }

    public static AntrianEntryModel Create(int noAntrian, RegReff reg, string reffId, string refDesc)
    {
        var newEntry = new AntrianEntryModel(noAntrian, AntrianStatusEnum.Taken, DateTime.Now,
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            reg, reffId, refDesc);
        return newEntry;
    }

    public static AntrianEntryModel Default =>
        new(-1, AntrianStatusEnum.Open, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            RegType.Default.ToReff(), "-", "");
    #endregion

    #region PROPERTIES
    public int NoAntrian { get; private set; }
    public AntrianStatusEnum AntrianStatus { get; private set; }
    public DateTime TakenAt { get; init; }
    public DateTime AssignedAt { get; private set; }
    public DateTime PreparedAt { get; private set; }
    public DateTime DeliveredAt { get; private set; }
    public DateTime CancelAt { get; private set; }

    public RegReff Reg { get; private set; }
    public string ReffId { get; private set; }
    public string ReffDesc { get; private set; }
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

    internal void SetReff(string reffId, string reffDesc)
    {
        ReffId = reffId;
        ReffDesc = reffDesc;
    }

    private void EnsureStatus(AntrianStatusEnum expected)
    {
        if (IsFinalState())
            throw new InvalidOperationException($"Status Antrian ({AntrianStatus}) cannot be change");

        if (AntrianStatus != expected)
            throw new InvalidOperationException($"Status Antrian must be {expected}");
    }

    private bool IsFinalState() =>
        AntrianStatus == AntrianStatusEnum.Delivered || AntrianStatus == AntrianStatusEnum.Cancelled;
    #endregion
}
