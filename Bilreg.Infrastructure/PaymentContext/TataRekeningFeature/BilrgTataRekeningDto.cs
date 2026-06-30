using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record BilrgTataRekeningDto(
    string RegId,
    int Status,
    string PetugasVerif,
    DateTime FinalizationDate)
{
    public static BilrgTataRekeningDto FromModel(TataRekeningModel model) =>
        new(
            model.RegId,
            (int)model.Status,
            model.FinalizationInfo.PetugasVerif,
            model.FinalizationInfo.FinalizationDate);

    public (TataRekeningStatusEnum Status, TataRekeningFinalizationType FinalizationInfo) ToHeaderParts() =>
        ((TataRekeningStatusEnum)Status, new TataRekeningFinalizationType(PetugasVerif, FinalizationDate));
}
