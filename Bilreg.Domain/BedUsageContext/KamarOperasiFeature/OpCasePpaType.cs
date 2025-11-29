using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record OpCasePpaType(
    int NoUrut,
    PpaReff Ppa,
    string Role,
    DateTime AssignDate)
{
    public static OpCasePpaType Default => new OpCasePpaType(0, PpaType.Default.ToReff(), "", new DateTime(3000, 1, 1));
}