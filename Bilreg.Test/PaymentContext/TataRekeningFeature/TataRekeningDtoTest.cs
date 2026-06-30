using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningDtoTest
{
    private static readonly DateTime FinalizationDate = new(2026, 6, 17, 14, 0, 0);

    [Fact]
    public void FromModel_MapsStatusAndFinalizationInfo()
    {
        var model = new TataRekeningModel(
            "REG-001",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("KSR-01", FinalizationDate),
            [],
            []);

        var dto = BilrgTataRekeningDto.FromModel(model);

        dto.RegId.Should().Be("REG-001");
        dto.Status.Should().Be((int)TataRekeningStatusEnum.Finalized);
        dto.PetugasVerif.Should().Be("KSR-01");
        dto.FinalizationDate.Should().Be(FinalizationDate);
    }

    [Fact]
    public void ToHeaderParts_MapsStatusAndFinalizationInfo()
    {
        var dto = new BilrgTataRekeningDto("REG-002", 1, "KSR-02", FinalizationDate);

        var (status, finalizationInfo) = dto.ToHeaderParts();

        status.Should().Be(TataRekeningStatusEnum.Closed);
        finalizationInfo.PetugasVerif.Should().Be("KSR-02");
        finalizationInfo.FinalizationDate.Should().Be(FinalizationDate);
    }
}
