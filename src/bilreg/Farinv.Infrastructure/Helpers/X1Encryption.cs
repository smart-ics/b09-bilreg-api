namespace Farinv.Infrastructure.Helpers;

public static class X1EncryptionHelper
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
