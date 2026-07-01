using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

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

public record StatusPesertaType(string StatusCode, string StatusDesc)
{
    public static StatusPesertaType Create(string statusCode)
    {
        var statusDesc = statusCode switch
        {
            "P" => "Peserta",
            "S" => "Suami",
            "I" => "Istri",
            "A" => "Anak",
            "O" => "Orang Tua",
            "X" => "Lainnya",
            _ => throw new ArgumentException("Invalid status code")
        };
        return new StatusPesertaType(statusCode, statusDesc);
    }
}

