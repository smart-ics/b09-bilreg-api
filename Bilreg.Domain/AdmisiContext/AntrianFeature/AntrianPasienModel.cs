namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianPasienModel
{
    public string AntrianId { get; init; }
    public string NoUrut { get; set; }
    public VisitorType Visitor { get; init; }
    public AntrianStatusEnum Status { get; init; }
}