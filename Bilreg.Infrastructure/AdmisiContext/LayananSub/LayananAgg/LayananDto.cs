using Bilreg.Domain.AdmisiContext.LayananSub;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananAgg;

public class LayananDto
{
    public string LayananId { get; set; }
    public string LayananName { get; set; }
    public bool IsAktif { get; set; }
    public string InstalasiId { get; set; }
    public string InstalasiName { get; set; }
    public string InstalasiDkId { get; set; }
    public string InstalasiDkName { get; set; }
    public string LayananDkId { get; set; }
    public string LayananDkName { get; set; }
    public string LayananTipeDkId { get; set; }
    public string LayananTipeDkName { get; set; }
    public string SmfId { get; set; }
    public string SmfName { get; set; }

    public LayananType ToModel()
    {
        var instalasiReff = new InstalasiReff(InstalasiId, InstalasiName);
        var instalasiDk = new InstalasiDkType(InstalasiDkId, InstalasiDkName);
        var layananDkReff = new LayananDkReff(LayananDkId, LayananDkName);
        var lynTipeDk = new TipeLayananDkType(LayananTipeDkId, LayananTipeDkName);

        var layanan = new LayananType(LayananId, LayananName, IsAktif,
            instalasiReff, instalasiDk, layananDkReff, lynTipeDk);
        return layanan;
    }

}
