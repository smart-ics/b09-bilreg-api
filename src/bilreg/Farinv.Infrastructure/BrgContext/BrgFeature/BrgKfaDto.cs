using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record BrgKfaDto(
    string BrgId,
    string KfaId,
    string KfaName
)
{
    public static BrgKfaDto FromModel(string brgId, KfaReff kfa)
    {
        return new BrgKfaDto(
            BrgId: brgId,
            KfaId: kfa.KfaId,
            KfaName: kfa.KfaName
        );
    }

    public KfaReff ToModel()
    {
        return new KfaReff(KfaId, KfaName);
    }
}