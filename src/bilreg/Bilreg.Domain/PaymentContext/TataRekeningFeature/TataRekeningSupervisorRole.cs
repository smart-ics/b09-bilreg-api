namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Katalog peran yang berwenang membatalkan finalisasi Tata Rekening.
/// </summary>
public static class TataRekeningSupervisorRole
{
    public const string VerifikatorSupervisor = "VERIF-SPV";
    public const string TataRekeningSupervisor = "TAREK-SPV";
    public const string SupervisorTataRekening = "Supervisor_TataRekening";
    public const string AdminKeuangan = "Admin_Keuangan";
    public const string RoleTataRekeningSupervisor = "Role_TataRekening_Supervisor";
    public const string RoleKeuanganAdmin = "Role_Keuangan_Admin";

    public static IReadOnlyList<string> All { get; } =
    [
        VerifikatorSupervisor,
        TataRekeningSupervisor,
        SupervisorTataRekening,
        AdminKeuangan,
        RoleTataRekeningSupervisor,
        RoleKeuanganAdmin
    ];

    public static bool IsSupervisor(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsAnySupervisor(IEnumerable<string>? roles) =>
        roles is not null && roles.Any(IsSupervisor);
}
