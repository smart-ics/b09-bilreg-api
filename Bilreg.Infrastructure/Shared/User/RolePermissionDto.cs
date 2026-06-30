namespace Bilreg.Infrastructure.Shared.User;

public record RolePermissionDto(string RoleId, string RoleName, string PermissionId, string PermissionName)
{
    public static IEnumerable<RolePermissionDto> ListData()
    {
        return new[]
        {
            // ================= ADM-SPV =================
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNWLK-CREATE","REG JALAN WALKIN CREATE"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNWLK-EDIT","REG JALAN WALKIN EDIT"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNWLK-VOID","REG JALAN WALKIN VOID"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNWLK-DELETE","REG JALAN WALKIN DELETE"),

            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNBOOK-CREATE","REG JALAN BOOKING CREATE"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNBOOK-EDIT","REG JALAN BOOKING EDIT"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNBOOK-VOID","REG JALAN BOOKING VOID"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","REGJLNBOOK-DELETE","REG JALAN BOOKING DELETE"),

            new RolePermissionDto("ADM-SPV","ADMISI SPV","BOK-CREATE","BOOKING CREATE"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","BOK-EDIT","BOOKING EDIT"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","BOK-VOID","BOOKING VOID"),
            new RolePermissionDto("ADM-SPV","ADMISI SPV","BOK-DELETE","BOOKING DELETE"),

            // ================= ADM-USR =================
            new RolePermissionDto("ADM-USR","ADMISI USER","REGJLNWLK-CREATE","REG JALAN WALKIN CREATE"),
            new RolePermissionDto("ADM-USR","ADMISI USER","REGJLNWLK-EDIT","REG JALAN WALKIN EDIT"),

            new RolePermissionDto("ADM-USR","ADMISI USER","REGJLNBOOK-CREATE","REG JALAN BOOKING CREATE"),
            new RolePermissionDto("ADM-USR","ADMISI USER","REGJLNBOOK-EDIT","REG JALAN BOOKING EDIT"),

            new RolePermissionDto("ADM-USR","ADMISI USER","BOK-CREATE","BOOKING CREATE"),
            new RolePermissionDto("ADM-USR","ADMISI USER","BOK-EDIT","BOOKING EDIT"),

            // ================= VERIF-SPV =================
            new RolePermissionDto("VERIF-SPV","VERIFIKATOR SPV","TATA-REKENING-READ","TATA REKENING READ"),
            new RolePermissionDto("VERIF-SPV","VERIFIKATOR SPV","TATA-REKENING-WRITE","TATA REKENING WRITE"),

            // ================= VERIF-USR =================
            new RolePermissionDto("VERIF-USR","VERIFIKATOR USER","TATA-REKENING-READ","TATA REKENING READ"),
            new RolePermissionDto("VERIF-USR","VERIFIKATOR USER","TATA-REKENING-WRITE","TATA REKENING WRITE"),
        };
    }
}