using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record BilrgTataRekeningDto(
    string RegId,
    int Status,
    string PetugasVerif,
    DateTime DischargeDate)
{
    public static BilrgTataRekeningDto FromModel(TataRekeningModel model) =>
        new(
            model.RegId,
            (int)model.Status,
            model.DischargeInfo.PetugasVerif,
            model.DischargeInfo.DischargeDate);

    public (TataRekeningStatusEnum Status, TataRekeningDischargeType DischargeInfo) ToHeaderParts() =>
        ((TataRekeningStatusEnum)Status, new TataRekeningDischargeType(PetugasVerif, DischargeDate));
}
