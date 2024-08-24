namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienModel
{
    public string PasienId { get; private set; }
    public string PasienName { get; private set; }
    public DateTime TglLahir { get; private set; }
    public string Gender { get; private set; }
    public DateTime TglMedrec { get; private set; }
    public string IbuKandung { get; private set; }
    public string GolDarah { get; private set; }
    
    //  TODO: Sampe disini
    public string StatusNikahId { get; private set; }
    public string StatusNikahName { get; private set; }
    
    public AlamatObj Alamat { get; private set; }
    public KelurahanObj Kelurahan { get; private set; }
    public IdentitasObj Identitas { get; private set; }
    public ContanctObj Contact { get; private set; }
}

public record AlamatObj(
    string Alamat, 
    string Alamat2, 
    string Alamat3, 
    string Kota, 
    string KodePos);
public record KelurahanObj(
    string Id,
    string Name, 
    string KecamatanName, 
    string KabupatenName, 
    string PropinsiName);

public record IdentitasObj(
    string JenisId,
    string NomorId,
    string NomorKk);

public record ContanctObj(
    string Email, 
    string NoTelp,
    string NoHp); 