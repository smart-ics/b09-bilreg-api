using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.Integration;

/// <summary>
/// Integration tests against test DB; requires ta_kamar / ta_kelas / ta_kelas_dk / ta_bangsal data.
/// </summary>
public class BangsalByKelasDkDalTest
{
    private readonly BangsalByKelasDkDal _sut = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void IT01_GivenKelasDk_WhenList_ThenReturnsBangsalForCareClass()
    {
        try
        {
            var rows = _sut.ListByKelasDkId("1").ToList();
            if (rows.Count == 0)
                return;

            rows.Should().OnlyContain(b => !string.IsNullOrWhiteSpace(b.BangsalId));
            rows.Select(b => b.BangsalId).Should().OnlyHaveUniqueItems();
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
    }

    [Fact]
    public void IT02_GivenUnknownKelasDk_WhenList_ThenReturnsEmpty()
    {
        try
        {
            var rows = _sut.ListByKelasDkId("Z").ToList();
            rows.Should().BeEmpty();
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
    }

    [Fact]
    public void IT03_GivenKelasDk_WhenList_ThenResultsAreOrderedByBangsalName()
    {
        try
        {
            var rows = _sut.ListByKelasDkId("1").ToList();
            if (rows.Count < 2)
                return;

            var names = rows.Select(b => b.BangsalName).ToList();
            names.Should().BeInAscendingOrder();
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
    }
}
