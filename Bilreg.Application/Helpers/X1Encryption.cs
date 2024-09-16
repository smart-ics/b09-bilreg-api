using FluentAssertions;
using Microsoft.VisualBasic;
using Xunit;

namespace Bilreg.Application.Helpers;

internal static class X1EncryptionHelper
{
    public static string Coding(string xKata)
    {
        var xHasil = "";
        int xLetak;
        var xSisa = 0;

        for (xLetak = 0; xLetak < xKata.Length; xLetak++)
        {
            xHasil += (char)((255 - (int)xKata[xLetak]) + xSisa);
            if (xLetak != xKata.Length - 1)
            {
                xSisa = (int)xHasil[xLetak] % 30;
            }
        }

        return xHasil;
    }
    public static string CodingNeo(string xKata)
    {
        var xHasil = "";
        var xSisa = 0;

        for (var xLetak = 0; xLetak < xKata.Length; xLetak++)
        {
            xHasil += (char)((255 - (int)xKata[xLetak]) + xSisa);
            if (xLetak != xKata.Length - 1)
            {
                xSisa = (int)xHasil[xLetak] % 20;
            }
        }

        return xHasil;
    }
    public static string Decoding(string xKata)
    {
        var xHasil = "";
        var xSisa = 0;

        for (var xLetak = 0; xLetak < xKata.Length; xLetak++)
        {
            xHasil += (char)(255 - ((int)xKata[xLetak] - xSisa));
            if (xLetak != xKata.Length - 1)
            {
                xSisa = (int)xKata[xLetak] % 30;
            }
        }

        return xHasil;
    }
    public static string DecodingNeo(string xKata)
    {
        var xHasil = "";
        var xSisa = 0;

        for (var xLetak = 0; xLetak < xKata.Length; xLetak++)
        {
            xHasil += (char)(255 - ((int)xKata[xLetak] - xSisa));
            if (xLetak != xKata.Length - 1)
            {
                xSisa = (int)xKata[xLetak] % 20;
            }
        }
        return xHasil;
    }
}

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