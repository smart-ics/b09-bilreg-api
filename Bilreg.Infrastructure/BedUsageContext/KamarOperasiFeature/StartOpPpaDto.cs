using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record StartOpPpaDto(string StartOpId, int NoUrut, string PpaId, 
    string ProfesiId, string GroupSpesialisId, string PpaName, string ProfesiName, string GroupSpesialisName)
{
    public static StartOpPpaDto FromModel (string startOpId, ScheduleOpPpaType model)
    {
        var result = new StartOpPpaDto(
            StartOpId: startOpId,
            NoUrut: model.NoUrut,
            PpaId: model.Ppa.PpaId,
            ProfesiId: model.Profesi.ProfesiId,
            GroupSpesialisId: model.GroupSpesialis.GroupSpesialisId,
            PpaName: model.Ppa.PpaName,
            ProfesiName: model.Profesi.ProfesiName,
            GroupSpesialisName: model.GroupSpesialis.GroupSpesialisName
            );
        return result;
    }

    public ScheduleOpPpaType ToModel()
    {
        var ppa = new PpaReff(PpaId, PpaName);
        var profesi = new ProfesiType(ProfesiId, ProfesiName);
        var groupSpesialis = new GroupSpesialisType(GroupSpesialisId, GroupSpesialisName);
        var result = new ScheduleOpPpaType(NoUrut, ppa, profesi, groupSpesialis);
        return result;
    }
}
