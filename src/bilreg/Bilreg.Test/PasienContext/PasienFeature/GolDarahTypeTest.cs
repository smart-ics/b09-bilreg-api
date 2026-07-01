using Bilreg.Domain.PasienContext.PasienFeature;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class GolDarahTypeTest
{
    [Fact]
    public void GivenA_WhenToString_ThenShouldReturnA()
    {
        var sut = new GolDarahType("A");
        Assert.Equal("A", sut.ToString());        
    }
}