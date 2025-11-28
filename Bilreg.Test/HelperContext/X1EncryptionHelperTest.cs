using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.HelperContext;

public class X1EncryptionHelperTest
{
    [Fact]
    public void CodingTest()
    {
        const string str = "3375022";
        var actual = X1EncryptionHelper.Coding(str);
        actual.Should().Be("ÌäÚÒÏèã");
    }
    [Fact]
    public void DecodingTest()
    {
        const string str = "ÌäÚÒÏèã";
        var actual = X1EncryptionHelper.Decoding(str);
        actual.Should().Be("3375022");
    }
    [Fact]
    public void CodeingNeoTest()
    {
        const string str = "3375022";
        var actual = X1EncryptionHelper.CodingNeo(str);
        actual.Should().Be("ÌÐÐÒÙÞÏ");
    }
    [Fact]
    public void DecodingNeoTest()
    {
        const string str = "ÌÐÐÒÙÞÏ";
        var actual = X1EncryptionHelper.DecodingNeo(str);
        actual.Should().Be("3375022");
    }
}