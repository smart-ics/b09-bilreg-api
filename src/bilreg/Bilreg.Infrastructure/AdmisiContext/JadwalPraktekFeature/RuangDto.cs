using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public record RuangDto
    (string RuangId, string RuangName, string PrefixAntrian)
{
    public static RuangDto FromModel(RuangType model)
    {
        return new RuangDto(
            model.RuangId, 
            model.RuangName, 
            model.PrefixAntrian);
    }

    public RuangType ToModel()
    {
        return new RuangType(RuangId, RuangName, PrefixAntrian);
    }
}