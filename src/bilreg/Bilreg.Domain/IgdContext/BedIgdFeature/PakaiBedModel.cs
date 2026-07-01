using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.BedIgdFeature;

public class PakaiBedModel : IPakaiBedKey
{
    private static readonly DateTime OPEN_SENTINEL = new(3000, 1, 1);
    private const string ID_PREFIX = "PKB";

    #region CREATION
    public PakaiBedModel(
        string pakaiBedId,
        string igdVisitId,
        string bedIgdId,
        string bedIgdName,
        DateTime checkInDateTime,
        string checkInUserId,
        DateTime checkOutDateTime,
        string checkOutUserId)
    {
        PakaiBedId = pakaiBedId;
        IgdVisitId = igdVisitId;
        BedIgdId = bedIgdId;
        BedIgdName = bedIgdName;
        CheckInDateTime = checkInDateTime;
        CheckInUserId = checkInUserId;
        CheckOutDateTime = checkOutDateTime;
        CheckOutUserId = checkOutUserId;
    }

    public static PakaiBedModel Default => new(
        pakaiBedId: "-",
        igdVisitId: "-",
        bedIgdId: "-",
        bedIgdName: "-",
        checkInDateTime: OPEN_SENTINEL,
        checkInUserId: "-",
        checkOutDateTime: OPEN_SENTINEL,
        checkOutUserId: "-");

    public static IPakaiBedKey Key(string id) => new PakaiBedModel(
        pakaiBedId: id,
        igdVisitId: "-",
        bedIgdId: "-",
        bedIgdName: "-",
        checkInDateTime: OPEN_SENTINEL,
        checkInUserId: "-",
        checkOutDateTime: OPEN_SENTINEL,
        checkOutUserId: "-");

    public static PakaiBedModel Open(IgdVisitModel visit, BedIgdModel bed, AuditInfoType audit)
    {
        Guard.Against.Null(visit);
        Guard.Against.Null(bed);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var newId = NunaId.New(ID_PREFIX);
        return new PakaiBedModel(
            pakaiBedId: newId,
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
    public string PakaiBedId { get; init; }
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
                $"PakaiBed {PakaiBedId} sudah ditutup pada {CheckOutDateTime:yyyy-MM-dd HH:mm}.");
        if (audit.Timestamp < CheckInDateTime)
            throw new ArgumentException(
                $"CheckOutDateTime ({audit.Timestamp}) tidak boleh lebih awal dari CheckInDateTime ({CheckInDateTime}).");

        CheckOutDateTime = audit.Timestamp;
        CheckOutUserId = audit.UserId;
    }
    #endregion
}
