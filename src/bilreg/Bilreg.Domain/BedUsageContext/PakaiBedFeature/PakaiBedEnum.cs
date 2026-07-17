namespace Bilreg.Domain.BedUsageContext.PakaiBedFeature;

public enum PakaiBedPurposeEnum
{
    Clinical = 1,
    Retained = 2,
    Companion = 3,
    RoomingIn = 4
}

public enum OccupantRoleEnum
{
    Primary = 1,
    Associated = 2,
    Companion = 3
}

public enum PakaiBedStatusEnum
{
    Proposed = 1,
    Active = 2,
    Released = 3,
    Cancelled = 4,
    EnteredInError = 5
}

public enum PakaiBedTransisiEnum
{
    Proposed = 1,
    Assigned = 2,
    ClinicalDesignated = 3,
    Retained = 4,
    Transferred = 5,
    Released = 6,
    RoomingInStarted = 7,
    RoomingInEnded = 8,
    Cancelled = 9,
    EnteredInError = 10
}

public enum PakaiBedKoreksiEnum
{
    Corrected = 1,
    EnteredInError = 2
}
