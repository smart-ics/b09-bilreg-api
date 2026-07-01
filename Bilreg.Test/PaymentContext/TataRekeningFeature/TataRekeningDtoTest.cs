using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningDtoTest
{
    private static readonly DateTime FinalizationDate = new(2026, 6, 17, 14, 0, 0);

    [Fact]
    public void FromModel_MapsStatusFinalizationAndPhase1Fields()
    {
        var model = new TataRekeningModel(
            "REG-001",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("KSR-01", FinalizationDate),
            [],
            [],
            FinancialVerificationStatusEnum.Valid,
            new FinancialVerificationInfo("VER-01", FinalizationDate),
            isFinancialResponsibilityAllocated: true,
            settlementInitiated: true,
            version: 4);

        var dto = BilrgTataRekeningDto.FromModel(model);

        dto.RegId.Should().Be("REG-001");
        dto.Status.Should().Be((int)TataRekeningStatusEnum.Finalized);
        dto.PetugasVerif.Should().Be("KSR-01");
        dto.FinalizationDate.Should().Be(FinalizationDate);
        dto.FinVerifStatus.Should().Be((int)FinancialVerificationStatusEnum.Valid);
        dto.IsAllocated.Should().BeTrue();
        dto.SettlementInitiated.Should().BeTrue();
        dto.Version.Should().Be(4);
    }

    [Fact]
    public void ToHeaderParts_MapsStatusAndFinalizationInfo()
    {
        var dto = TataRekeningDtoTestHelper.BuildHeaderDto("REG-002", TataRekeningStatusEnum.Closed) with
        {
            PetugasVerif = "KSR-02",
            FinalizationDate = FinalizationDate
        };

        var (status, finalizationInfo) = dto.ToHeaderParts();

        status.Should().Be(TataRekeningStatusEnum.Closed);
        finalizationInfo.PetugasVerif.Should().Be("KSR-02");
        finalizationInfo.FinalizationDate.Should().Be(FinalizationDate);
    }

    [Fact]
    public void ToPhase1Parts_MapsVerificationAllocationSettlementAndVersion()
    {
        var dto = TataRekeningDtoTestHelper.BuildHeaderDto("REG-003") with
        {
            FinVerifStatus = (int)FinancialVerificationStatusEnum.Valid,
            FinVerifPetugas = "VER-03",
            FinVerifDate = FinalizationDate,
            IsAllocated = true,
            SettlementInitiated = true,
            Version = 7
        };

        var (finVerifStatus, finVerifInfo, isAllocated, settlementInitiated, version) = dto.ToPhase1Parts();

        finVerifStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
        finVerifInfo!.PetugasVerif.Should().Be("VER-03");
        isAllocated.Should().BeTrue();
        settlementInitiated.Should().BeTrue();
        version.Should().Be(7);
    }
}
