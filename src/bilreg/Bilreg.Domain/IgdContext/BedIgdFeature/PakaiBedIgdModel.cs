using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.BedIgdFeature;

public class PakaiBedIgdModel : IPakaiBedIgdKey
{
    private static readonly DateTime OPEN_SENTINEL = new(3000, 1, 1);
    private const string ID_PREFIX = "PBI";

    #region CREATION
    public PakaiBedIgdModel(
        string pakaiBedIgdId,
        string igdVisitId,
        string bedIgdId,
        string bedIgdName,
        DateTime checkInDateTime,
        string checkInUserId,
        DateTime checkOutDateTime,
        string checkOutUserId)
    {
        PakaiBedIgdId = pakaiBedIgdId;
        IgdVisitId = igdVisitId;
        BedIgdId = bedIgdId;
        BedIgdName = bedIgdName;
        CheckInDateTime = checkInDateTime;
        CheckInUserId = checkInUserId;
        CheckOutDateTime = checkOutDateTime;
        CheckOutUserId = checkOutUserId;
    }

    public static PakaiBedIgdModel Default => new(
        pakaiBedIgdId: "-",
        igdVisitId: "-",
        bedIgdId: "-",
        bedIgdName: "-",
        checkInDateTime: OPEN_SENTINEL,
        checkInUserId: "-",
        checkOutDateTime: OPEN_SENTINEL,
        checkOutUserId: "-");

    public static IPakaiBedIgdKey Key(string id) => new PakaiBedIgdModel(
        pakaiBedIgdId: id,
        igdVisitId: "-",
        bedIgdId: "-",
        bedIgdName: "-",
        checkInDateTime: OPEN_SENTINEL,
        checkInUserId: "-",
        checkOutDateTime: OPEN_SENTINEL,
        checkOutUserId: "-");

    public static PakaiBedIgdModel Open(IgdVisitModel visit, BedIgdModel bed, AuditInfoType audit)
    {
        Guard.Against.Null(visit);
        Guard.Against.Null(bed);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var newId = NunaId.New(ID_PREFIX);
        return new PakaiBedIgdModel(
            pakaiBedIgdId: newId,
            igdVisitId: visit.IgdVisitId,
            bedIgdId: bed.BedIgdId,
            bedIgdName: bed.BedIgdName,
            checkInDateTime: audit.Timestamp,
            checkInUserId: audit.UserId,
            checkOutDateTime: OPEN_SENTINEL,
            checkOutUserId: "-");
    }
    #endregion

    #region PROPERTIES
    public string PakaiBedIgdId { get; init; }
    public string IgdVisitId { get; init; }
    public string BedIgdId { get; init; }
    public string BedIgdName { get; init; }
    public DateTime CheckInDateTime { get; init; }
    public string CheckInUserId { get; init; }
    public DateTime CheckOutDateTime { get; private set; }
    public string CheckOutUserId { get; private set; }

    public bool IsOpen => CheckOutDateTime == OPEN_SENTINEL;
    #endregion

    #region BEHAVIOUR
    public void Close(AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));
        if (!IsOpen)
            throw new InvalidOperationException(
                $"PakaiBedIgd {PakaiBedIgdId} sudah ditutup pada {CheckOutDateTime:yyyy-MM-dd HH:mm}.");
        if (audit.Timestamp < CheckInDateTime)
            throw new ArgumentException(
                $"CheckOutDateTime ({audit.Timestamp}) tidak boleh lebih awal dari CheckInDateTime ({CheckInDateTime}).");

        CheckOutDateTime = audit.Timestamp;
        CheckOutUserId = audit.UserId;
    }
    #endregion
}
