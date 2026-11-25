using System.Text;

namespace Bilreg.Infrastructure.Shared.Helpers;

public static class X1EncryptionHelper
{
    private static readonly Encoding Windows1252;

    static X1EncryptionHelper()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Windows1252 = Encoding.GetEncoding(1252);
    }

    public static string Coding(string xKata)
    {
        if (string.IsNullOrEmpty(xKata)) return string.Empty;

        var bytes = Windows1252.GetBytes(xKata);
        var outBytes = new byte[bytes.Length];
        var xSisa = 0;

        for (var xLetak = 0; xLetak < bytes.Length; xLetak++)
        {
            var val = (255 - bytes[xLetak]) + xSisa;
            outBytes[xLetak] = (byte)val;
            if (xLetak != bytes.Length - 1)
            {
                xSisa = val % 30;
            }
        }

        return Windows1252.GetString(outBytes);
    }

    public static string CodingNeo(string xKata)
    {
        if (string.IsNullOrEmpty(xKata)) return string.Empty;

        var bytes = Windows1252.GetBytes(xKata);
        var outBytes = new byte[bytes.Length];
        var xSisa = 0;

        for (var xLetak = 0; xLetak < bytes.Length; xLetak++)
        {
            var val = (255 - bytes[xLetak]) + xSisa;
            outBytes[xLetak] = (byte)val;
            if (xLetak != bytes.Length - 1)
            {
                xSisa = val % 20;
            }
        }

        return Windows1252.GetString(outBytes);
    }

    public static string Decoding(string xKata)
    {
        if (string.IsNullOrEmpty(xKata)) return string.Empty;

        var bytes = Windows1252.GetBytes(xKata);
        var sb = new StringBuilder(bytes.Length);
        var xSisa = 0;

        for (var xLetak = 0; xLetak < bytes.Length; xLetak++)
        {
            var b = (int)bytes[xLetak];
            var charVal = (char)(255 - (b - xSisa));
            sb.Append(charVal);
            if (xLetak != bytes.Length - 1)
            {
                xSisa = b % 30;
            }
        }

        return sb.ToString();
    }

    public static string DecodingNeo(string xKata)
    {
        if (string.IsNullOrEmpty(xKata)) return string.Empty;

        var bytes = Windows1252.GetBytes(xKata);
        var sb = new StringBuilder(bytes.Length);
        var xSisa = 0;

        for (var xLetak = 0; xLetak < bytes.Length; xLetak++)
        {
            var b = (int)bytes[xLetak];
            var charVal = (char)(255 - (b - xSisa));
            sb.Append(charVal);
            if (xLetak != bytes.Length - 1)
            {
                xSisa = b % 20;
            }
        }

        return sb.ToString();
    }
}
