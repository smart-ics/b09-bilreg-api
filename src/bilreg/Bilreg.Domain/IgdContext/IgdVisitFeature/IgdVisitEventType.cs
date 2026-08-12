namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public enum IgdEventEnum
{
    Daftar = 0,
    AssignDokter = 1,
    AssessTriage = 2,
    AssignBed = 3,
    CheckOut = 4,
    AssignRegister = 5,
    Redirect = 6,
    Discharge = 7,
    Void = 8,
    ClearBed = 9,
    AddTindakan = 10,
    AddBhp = 11,
    TransferBed = 12,
    VoidTindakan = 13,
    ReplaceRegister = 14,
}

public static class IgdEventEnumExtensions
{
    public static string ToCode(this IgdEventEnum kind) => kind switch
    {
        IgdEventEnum.Daftar => "DAFTAR",
        IgdEventEnum.AssignDokter => "ASSIGN_DOKTER",
        IgdEventEnum.AssessTriage => "ASSESS_TRIAGE",
        IgdEventEnum.AssignBed => "ASSIGN_BED",
        IgdEventEnum.CheckOut => "CHECK_OUT",
        IgdEventEnum.AssignRegister => "ASSIGN_REGISTER",
        IgdEventEnum.Redirect => "REDIRECT",
        IgdEventEnum.Discharge => "DISCHARGE",
        IgdEventEnum.Void => "VOID",
        IgdEventEnum.ClearBed => "CLEAR_BED",
        IgdEventEnum.AddTindakan => "ADD_TINDAKAN",
        IgdEventEnum.AddBhp => "ADD_BHP",
        IgdEventEnum.TransferBed => "TRANSFER_BED",
        IgdEventEnum.VoidTindakan => "VOID_TINDAKAN",
        IgdEventEnum.ReplaceRegister => "REPLACE_REGISTER",
        _ => "DAFTAR",
    };

    public static IgdEventEnum ToIgdEventEnum(this string code) => code switch
    {
        "DAFTAR" => IgdEventEnum.Daftar,
        "ASSIGN_DOKTER" => IgdEventEnum.AssignDokter,
        "ASSESS_TRIAGE" => IgdEventEnum.AssessTriage,
        "ASSIGN_BED" => IgdEventEnum.AssignBed,
        "CHECK_OUT" => IgdEventEnum.CheckOut,
        "ASSIGN_REGISTER" => IgdEventEnum.AssignRegister,
        "REDIRECT" => IgdEventEnum.Redirect,
        "DISCHARGE" => IgdEventEnum.Discharge,
        "VOID" => IgdEventEnum.Void,
        "CLEAR_BED" => IgdEventEnum.ClearBed,
        "ADD_TINDAKAN" => IgdEventEnum.AddTindakan,
        "ADD_BHP" => IgdEventEnum.AddBhp,
        "TRANSFER_BED" => IgdEventEnum.TransferBed,
        "VOID_TINDAKAN" => IgdEventEnum.VoidTindakan,
        "REPLACE_REGISTER" => IgdEventEnum.ReplaceRegister,
        _ => IgdEventEnum.Daftar,
    };
}

public record IgdVisitEventType(
    int NoEvent,
    IgdEventEnum EventKind,
    DateTime EventDateTime,
    string UserId,
    string Notes)
{
    public static IgdVisitEventType Default => new(
        NoEvent: 0,
        EventKind: IgdEventEnum.Daftar,
        EventDateTime: new DateTime(3000, 1, 1),
        UserId: "-",
        Notes: "-");
}
