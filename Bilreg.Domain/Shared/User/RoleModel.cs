using System.Security;

namespace Bilreg.Domain.Shared.User;

public class RoleModel : IRoleKey
{
    #region CREATION
    public RoleModel(string id, string name)
    {
        RoleId = id;
        RoleName = name;
    }
    public static IRoleKey Key(string id) => new RoleModel(id, "-");
    public static RoleModel Default => new RoleModel("-", "-");
    #endregion

    #region PROPERTIES
    public string RoleId { get; private set; }
    public string RoleName { get; private set; }
    #endregion

    #region BEHAVIOR
    public IEnumerable<RoleModel> ListData()
    {
        return new[]
        {
            new RoleModel("ADM-SPV",  "Admisi SPV"),
            new RoleModel("ADM-USER", "Admisi USER"),

            new RoleModel("TAREK-SPV",  "Tata Rekening SPV"),
            new RoleModel("TAREK-USER", "Tata Rekening USER"),

            new RoleModel("KSR-SPV",  "Kasir SPV"),
            new RoleModel("KSR-USER", "Kasir USER"),

            new RoleModel("RM-SPV",  "Rekam Medis SPV"),
            new RoleModel("RM-USER", "Rekam Medis USER"),

            new RoleModel("PRJ-SPV",  "Poli Rawat Jalan SPV"),
            new RoleModel("PRJ-USER", "Poli Rawat Jalan USER"),

            new RoleModel("BRI-SPV",  "Bangsal Rawat Inap SPV"),
            new RoleModel("BRI-USER", "Bangsal Rawat Inap USER"),

            new RoleModel("IGD-SPV",  "IGD SPV"),
            new RoleModel("IGD-USER", "IGD USER"),

            new RoleModel("LAB-SPV",  "Laboratorium SPV"),
            new RoleModel("LAB-USER", "Laboratorium USER"),

            new RoleModel("RAD-SPV",  "Radiologi SPV"),
            new RoleModel("RAD-USER", "Radiologi USER"),

            new RoleModel("KO-SPV",  "Kamar Operasi SPV"),
            new RoleModel("KO-USER", "Kamar Operasi USER"),

            new RoleModel("APT-SPV",  "Apotek SPV"),
            new RoleModel("APT-USER", "Apotek USER"),

            new RoleModel("GF-SPV",  "Gudang Farmasi SPV"),
            new RoleModel("GF-USER", "Gudang Farmasi USER"),

            new RoleModel("PB-SPV",  "Pengadaan Barang SPV"),
            new RoleModel("PB-USER", "Pengadaan Barang USER"),
        };
    }
    #endregion

}


public interface IRoleKey
{ string RoleId { get; } }
