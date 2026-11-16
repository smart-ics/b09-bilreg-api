using Bilreg.Domain.InventoryContext;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext;

public class InventoryLocationIdsTest
{
    [Fact]
    public void LogisticsTransitLayananId_ShouldBeTRANS_AndNotExceedFiveCharacters()
    {
        // Act & Assert
        InventoryLocationIds.LogisticsTransitLayananId.Should().Be("TRANS");
        InventoryLocationIds.LogisticsTransitLayananId.Length.Should().BeLessOrEqualTo(5);
    }
}
