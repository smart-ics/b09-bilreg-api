using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpPpaDto(string ScheduleOpId, int NoUrut, string PpaId, 
    string ProfesiId, string GroupSpesialisId, string PpaName, string ProfesiName, string GroupSpesialisName)
{
    public static ScheduleOpPpaDto FromModel(string scheduleOpId, ScheduleOpPpaType model)
    {
        var result = new ScheduleOpPpaDto(scheduleOpId, model.NoUrut, model.Ppa.PpaId, 
            model.Profesi.ProfesiId, model.GroupSpesialis.GroupSpesialisId, 
            model.Ppa.PpaName, model.Profesi.ProfesiName, model.GroupSpesialis.GroupSpesialisName);
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