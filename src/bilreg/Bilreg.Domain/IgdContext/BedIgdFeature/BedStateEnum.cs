namespace Bilreg.Domain.IgdContext.BedIgdFeature;

public enum BedStateEnum
{
    Active = 0,
    Occupied = 1,
    Maintenance = 2,
    Dirty = 3,
}

public static class BedStateEnumExtensions
{
    public static string ToCode(this BedStateEnum state) => state switch
    {
        BedStateEnum.Active => "ACTIVE",
        BedStateEnum.Occupied => "OCCUPIED",
        BedStateEnum.Maintenance => "MAINTENANCE",
        BedStateEnum.Dirty => "DIRTY",
        _ => "ACTIVE",
    };

    public static BedStateEnum ToBedStateEnum(this string code) => code switch
    {
        "ACTIVE" => BedStateEnum.Active,
        "OCCUPIED" => BedStateEnum.Occupied,
        "MAINTENANCE" => BedStateEnum.Maintenance,
        "DIRTY" => BedStateEnum.Dirty,
        _ => BedStateEnum.Active,
    };
}
