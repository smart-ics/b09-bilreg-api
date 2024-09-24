using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public partial class PolisCoverModel
{
    internal void WithPasien(PasienModel pasien)
    {
        ArgumentNullException.ThrowIfNull(pasien);
        ArgumentException.ThrowIfNullOrWhiteSpace(pasien.PasienId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pasien.PasienName);
        
        PasienId = pasien.PasienId;
        PasienName = pasien.PasienName;
    }

    internal void SetStatusPeserta(string statusPeserta)
    {
        var validChar = new[] { "P", "S", "I", "A", "O", "X" };
        if (!validChar.Contains(statusPeserta))
            throw new ArgumentException($"Invalid status peserta: {statusPeserta}");
        Status = statusPeserta;
        StatusDesc = statusPeserta switch
        {
            "P" => "Peserta",
            "S" => "Suami",
            "I" => "Istri",
            "A" => "Anak",
            "O" => "Orang Tua",
            "X" => "Lainnya",
            _ => string.Empty
        };
    }

    internal void SetId(string id) => PolisId = id;
}