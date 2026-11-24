using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WaitingListFeature;

public class WaitingListWorklistDalTest
{
    private readonly WaitingListWorklistDal _sut = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void ListActiveWorklistTest()
    {
        // Arrange
        using var trans = TransHelper.NewScope();

        // Act
        var act = () => _sut.ListActiveWorklist("B01");

        // Assert
        act.Should().NotThrow();
        var result = act();
        result.Should().NotBeNull();
    }

    [Fact]
    public void List_GivenFilter_ExecutesQueryWithoutError()
    {
        // Arrange
        using var trans = TransHelper.NewScope();

        // Act & Assert - Specific filter with BangsalId and Status
        var actSpecific = () => _sut.List(new WaitingListWorklistFilter("B01", 0));
        actSpecific.Should().NotThrow();
        var specificResult = actSpecific();
        specificResult.Should().NotBeNull();

        // Act & Assert - Filter with null properties
        var actDefaultFilter = () => _sut.List(new WaitingListWorklistFilter());
        actDefaultFilter.Should().NotThrow();
        var defaultFilterResult = actDefaultFilter();
        defaultFilterResult.Should().NotBeNull();

        // Act & Assert - Null filter object
        var actNullFilter = () => _sut.List(null!);
        actNullFilter.Should().NotThrow();
        var nullFilterResult = actNullFilter();
        nullFilterResult.Should().NotBeNull();
    }
}
