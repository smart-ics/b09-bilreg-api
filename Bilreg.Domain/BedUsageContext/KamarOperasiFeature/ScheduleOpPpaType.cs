using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpPpaType(int NoUrut, PpaReff Ppa, ProfesiType Profesi, GroupSpesialisType GroupSpesialis)
{
    public static ScheduleOpPpaType Default => new ScheduleOpPpaType(0, PpaType.Default.ToReff(), ProfesiType.Default, GroupSpesialisType.Default);
}
