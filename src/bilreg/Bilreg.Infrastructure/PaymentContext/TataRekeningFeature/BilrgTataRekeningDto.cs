using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record BilrgTataRekeningDto(
    string RegId,
    int Status,
    string PetugasVerif,
    DateTime FinalizationDate,
    int FinVerifStatus,
    string FinVerifPetugas,
    DateTime FinVerifDate,
    bool IsAllocated,
    bool SettlementInitiated,
    int Version)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static BilrgTataRekeningDto FromModelForInsert(TataRekeningModel model) =>
        FromModel(model) with { Version = 1 };

    public static BilrgTataRekeningDto FromModel(TataRekeningModel model)
    {
        var finVerifInfo = model.FinancialVerificationInfo;
        return new BilrgTataRekeningDto(
            model.RegId,
            (int)model.Status,
            Truncate(model.FinalizationInfo.PetugasVerif, 10),
            model.FinalizationInfo.FinalizationDate,
            (int)model.FinancialVerificationStatus,
            Truncate(finVerifInfo?.PetugasVerif, 10),
            finVerifInfo?.VerifiedAt ?? EmptyDate,
            model.IsFinancialResponsibilityAllocated,
            model.SettlementInitiated,
            model.Version);
    }

    public (TataRekeningStatusEnum Status, TataRekeningFinalizationType FinalizationInfo) ToHeaderParts() =>
        ((TataRekeningStatusEnum)Status, new TataRekeningFinalizationType(PetugasVerif, FinalizationDate));

    public (
        FinancialVerificationStatusEnum FinVerifStatus,
        FinancialVerificationInfo? FinVerifInfo,
        bool IsAllocated,
        bool SettlementInitiated,
        int Version) ToPhase1Parts()
    {
        FinancialVerificationInfo? finVerifInfo = FinVerifStatus == (int)FinancialVerificationStatusEnum.Valid &&
                                                  FinVerifDate != EmptyDate
            ? new FinancialVerificationInfo(FinVerifPetugas, FinVerifDate)
            : null;

        return (
            (FinancialVerificationStatusEnum)FinVerifStatus,
            finVerifInfo,
            IsAllocated,
            SettlementInitiated,
            Version);
    }
}
