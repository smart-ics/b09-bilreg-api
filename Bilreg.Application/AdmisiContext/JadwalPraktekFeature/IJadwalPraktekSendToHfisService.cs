using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public interface IJadwalPraktekSendToHfisService :
    INunaService<IEnumerable<JadwalPraktekSendHfisResponse>, JadwalPraktekSendHfisPayload>;

public record JadwalPraktekSendHfisPayload(string RSID,
    IEnumerable<ItemJadwalPraktekHfis> ListJadwalPraktek);

public record ItemJadwalPraktekHfis(string DokterID,
    int Hari,
    string JamMulai,
    string JamSelesai,
    int LimitAll,
    int LimitBpjs);



public record JadwalPraktekSendHfisResponse(string RSID,
    string DokterID,
    int Hari,
    string JamMulai,
    string JamMulaiJKN,
    string RSName,
    string DokterName,
    string LayananID,
    string LayananName,
    string JamSelesai,
    string JamSelesaiJKN,
    int LimitAll,
    int LimitBpjs,
    string SubSpesialisID,
    string SubSpesialisName,
    string PoliID,
    string PoliName)
{
    public static JadwalPraktekSendHfisResponse Default => 
        new("-", "-", 0, "", "", "", "", "", "", "", "", 0, 0, "", "", "", "");
}
