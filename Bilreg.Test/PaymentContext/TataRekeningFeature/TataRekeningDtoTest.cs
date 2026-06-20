using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningDtoTest
{
    private static readonly DateTime DischargeDate = new(2026, 6, 17, 14, 0, 0);

    [Fact]
    public void FromModel_MapsStatusAndDischargeInfo()
    {
        var model = new TataRekeningModel(
            "REG-001",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningDischargeType("KSR-01", DischargeDate),
            [],
            []);

        var dto = BilrgTataRekeningDto.FromModel(model);

        dto.RegId.Should().Be("REG-001");
        dto.Status.Should().Be((int)TataRekeningStatusEnum.Finalized);
        dto.PetugasVerif.Should().Be("KSR-01");
        dto.DischargeDate.Should().Be(DischargeDate);
    }

    [Fact]
    public void ToHeaderParts_MapsStatusAndDischargeInfo()
    {
        var dto = new BilrgTataRekeningDto("REG-002", 1, "KSR-02", DischargeDate);

        var (status, dischargeInfo) = dto.ToHeaderParts();

        status.Should().Be(TataRekeningStatusEnum.Closed);
        dischargeInfo.PetugasVerif.Should().Be("KSR-02");
        dischargeInfo.DischargeDate.Should().Be(DischargeDate);
    }
}
