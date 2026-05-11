namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public enum AdministrativeStateEnum
{
    Daftar = 0,
    Registered = 1,
    Redirected = 2,
    Discharged = 3,
}

public static class AdministrativeStateEnumExtensions
{
    public static string ToCode(this AdministrativeStateEnum state) => state switch
    {
        AdministrativeStateEnum.Daftar => "DAFTAR",
        AdministrativeStateEnum.Registered => "REGISTERED",
        AdministrativeStateEnum.Redirected => "REDIRECTED",
        AdministrativeStateEnum.Discharged => "DISCHARGED",
        _ => "DAFTAR",
    };

    public static AdministrativeStateEnum ToAdministrativeStateEnum(this string code) => code switch
    {
        "DAFTAR" => AdministrativeStateEnum.Daftar,
        "REGISTERED" => AdministrativeStateEnum.Registered,
        "REDIRECTED" => AdministrativeStateEnum.Redirected,
        "DISCHARGED" => AdministrativeStateEnum.Discharged,
        _ => AdministrativeStateEnum.Daftar,
    };
}
