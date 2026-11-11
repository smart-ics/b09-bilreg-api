namespace Bilreg.Domain.ApotekContext.QueueFeature;

public enum ApotekPhaseEnum
{
    TAKEN = 1,
    PENERIMAAN = 2,
    PENYIAPAN = 3,
    SIAP_SERAH = 4,
    SERAH_OBAT = 5,
    SELESAI = 6,
    DILEWATI = 7,
    BATAL = 8,
    NO_SHOW = 9
}

public enum LoketTypeEnum
{
    None = 0,
    PenerimaanResep = 1,
    SerahObat = 2
}

public enum LoketClaimStateEnum
{
    Idle = 0,
    Called = 1,
    InService = 2
}

public interface IApotekQueueEntryKey
{
    string AntrianId { get; }
    int NoUrut { get; }
}

public interface IPharmacyServicePointKey
{
    string ServicePointId { get; }
}
