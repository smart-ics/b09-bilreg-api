using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public record PolisCoverModel : IPolisKey
{
    public PolisCoverModel(string polisId, PasienReff pasien, StatusPesertaType status)
    {
        PolisId = polisId;
        Pasien = pasien;
        Status = status;
    }
    public string PolisId { get; init; }
    public PasienReff Pasien { get; init; }
    public StatusPesertaType Status { get; init; }
    public DateTime ExpiredDate { get; protected set; }
}

public record StatusPesertaType(string Status, string StatusDesc)
{
    public static StatusPesertaType Peserta => new("P", "Peserta");
    public static StatusPesertaType Suami => new("S", "Suami");
    public static StatusPesertaType Istri => new("I", "Istri");
    public static StatusPesertaType Anak => new("A", "Anak");
    public static StatusPesertaType OrangTua => new("O", "Orang Tua");
    public static StatusPesertaType Lainnya => new("X", "Lainnya");
}

