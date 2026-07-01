using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCasePpaDto(string OrderOpId,
    int NoUrut, string PpaId, string Role, DateTime AssignDate,
    string PpaName)
{
    public static OpCasePpaDto FromModel(string orderOpId, OpCasePpaType model)
    {
        var result = new OpCasePpaDto(orderOpId, model.NoUrut, model.Ppa.PpaId, model.Role, 
            model.AssignDate, model.Ppa.PpaName);
        return result;
    }
    
    public OpCasePpaType ToModel()
    {
        var ppa = new PpaReff(PpaId, PpaName);
        var result = new OpCasePpaType(NoUrut, ppa, Role, AssignDate);
        return result;
    }
}