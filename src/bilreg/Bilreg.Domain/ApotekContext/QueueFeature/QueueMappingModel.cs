using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.QueueFeature;

public enum QueueDemandKindEnum
{
    ResepKerja = 0,
    JualBebas = 1
}

public enum QueueMappingMethodEnum
{
    Tracker = 0,
    Manual = 1
}

public interface IQueueMappingKey
{
    QueueDemandKindEnum DemandKind { get; }
    string DemandId { get; }
}

public class QueueMappingModel : IQueueMappingKey
{
    private QueueMappingModel(
        QueueDemandKindEnum demandKind,
        string demandId,
        string antrianId,
        int noUrut,
        string pasienTrackerId,
        QueueMappingMethodEnum mappingMethod,
        string mappedBy,
        DateTime mappedAt)
    {
        DemandKind = demandKind;
        DemandId = demandId;
        AntrianId = antrianId;
        NoUrut = noUrut;
        PasienTrackerId = pasienTrackerId ?? "";
        MappingMethod = mappingMethod;
        MappedBy = mappedBy;
        MappedAt = mappedAt;
    }

    public static IQueueMappingKey Key(QueueDemandKindEnum demandKind, string demandId)
        => new MappingKey(demandKind, demandId);

    public static QueueMappingModel Create(
        QueueDemandKindEnum demandKind,
        string demandId,
        string antrianId,
        int noUrut,
        string pasienTrackerId,
        QueueMappingMethodEnum mappingMethod,
        string mappedBy,
        DateTime mappedAt)
    {
        Guard.Against.NullOrWhiteSpace(demandId, nameof(demandId));
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        Guard.Against.NullOrWhiteSpace(mappedBy, nameof(mappedBy));
        return new QueueMappingModel(demandKind, demandId, antrianId, noUrut, pasienTrackerId, mappingMethod, mappedBy, mappedAt);
    }

    public void Correct(string antrianId, int noUrut, string pasienTrackerId, string mappedBy, DateTime mappedAt)
    {
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        AntrianId = antrianId;
        NoUrut = noUrut;
        PasienTrackerId = pasienTrackerId ?? "";
        MappedBy = mappedBy;
        MappedAt = mappedAt;
    }

    public QueueDemandKindEnum DemandKind { get; }
    public string DemandId { get; }
    public string AntrianId { get; private set; }
    public int NoUrut { get; private set; }
    public string PasienTrackerId { get; private set; }
    public QueueMappingMethodEnum MappingMethod { get; }
    public string MappedBy { get; private set; }
    public DateTime MappedAt { get; private set; }

    private sealed record MappingKey(QueueDemandKindEnum DemandKind, string DemandId) : IQueueMappingKey;
}

public interface IQueueCloseKey
{
    string QueueCloseId { get; }
}

public class QueueCloseModel : IQueueCloseKey
{
    public const string IdPrefix = "AQC";

    private QueueCloseModel(
        string queueCloseId,
        string antrianId,
        int noUrut,
        string reason,
        string staffId,
        DateTime effectiveAt)
    {
        QueueCloseId = queueCloseId;
        AntrianId = antrianId;
        NoUrut = noUrut;
        Reason = reason;
        StaffId = staffId;
        EffectiveAt = effectiveAt;
    }

    public static IQueueCloseKey Key(string queueCloseId) => new CloseKey(queueCloseId);

    public static QueueCloseModel Create(string antrianId, int noUrut, string reason, string staffId, DateTime effectiveAt)
    {
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(staffId, nameof(staffId));
        return new QueueCloseModel(NunaId.New(IdPrefix), antrianId, noUrut, reason, staffId, effectiveAt);
    }

    public static QueueCloseModel Rehydrate(
        string queueCloseId, string antrianId, int noUrut, string reason, string staffId, DateTime effectiveAt)
        => new(queueCloseId, antrianId, noUrut, reason, staffId, effectiveAt);

    public string QueueCloseId { get; }
    public string AntrianId { get; }
    public int NoUrut { get; }
    public string Reason { get; }
    public string StaffId { get; }
    public DateTime EffectiveAt { get; }

    private sealed record CloseKey(string QueueCloseId) : IQueueCloseKey;
}
