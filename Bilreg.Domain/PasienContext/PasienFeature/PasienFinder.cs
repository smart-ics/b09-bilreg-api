using System.Globalization;
using System.Text.RegularExpressions;
using Bilreg.Domain.Helpers;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public record PasienFinder(
    string PasienId,
    string TglLahir,
    string RegId,
    string BookingId,
    string[] StringVariants)
{
    public static PasienFinder CreateNew(string keyword, string pasienIdPrefix)
    {
        keyword = keyword.ToUpper();
        if (string.IsNullOrWhiteSpace(keyword))
            return new PasienFinder("", "", "", "", []);

        var tokens = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var pasienId = "";
        var regId = "";
        var bookingId = "";
        var tglLahir = "";
        string[] stringVariants = [];

        foreach (var token in tokens)
        {
            var isFormattedData = false;
            if (string.IsNullOrEmpty(pasienId))
            {
                pasienId = TryParsePasienId(token, pasienIdPrefix);
                isFormattedData = isFormattedData ? isFormattedData : pasienId != string.Empty;
            }

            if (string.IsNullOrEmpty(regId))
            {
                regId = TryParseRegisterId(token);
                isFormattedData = isFormattedData ? isFormattedData : regId != string.Empty;
            }

            if (string.IsNullOrEmpty(bookingId))
            {
                bookingId = TryParseBookingId(token);
                isFormattedData = isFormattedData ? isFormattedData : bookingId != string.Empty;;
            }

            if (string.IsNullOrEmpty(tglLahir))
            {
                tglLahir = TryParseDate(token);
                isFormattedData = isFormattedData ? isFormattedData : tglLahir != string.Empty;
            }

            if (isFormattedData) continue;
            
            var variants = MutateStringVariants(token);
            stringVariants = stringVariants.Concat(variants).ToArray();
        }

        return new PasienFinder(
            PasienId: pasienId,
            TglLahir: tglLahir,
            RegId: regId,
            BookingId: bookingId,
            stringVariants);
    }

    // ========== PRIVATE HELPERS ==========
    private static string[] MutateStringVariants(string token)
    {
        var result = new string[] { token };
        //  convert to eyd
        var eyd = token.ToEyd();
        if (token != eyd)
            result = result.Append(eyd).ToArray();
        
        //  convert to ejaan lama
        var ejaanLama = token.ToEjaanLama();
        if (token != ejaanLama)
            result = result.Append(ejaanLama).ToArray();
        
        //  normalize
        var normalize = token.ToNormal();
        if (token != normalize)
            result = result.Append(normalize).ToArray();

        //  remove double char
        var removedDoubleChar = token.RemoveDoubleChar();
        if (token != removedDoubleChar)
            result = result.Append(removedDoubleChar).ToArray();
        
        return result;
    }
    
    private static string TryParseRegisterId(string token)
    {
        if (token.StartsWith("RG", StringComparison.OrdinalIgnoreCase))
        {
            var numberPart = token[2..];
            if (int.TryParse(numberPart, out var num))
                return $"RG{num:D8}";
        }

        if (Regex.IsMatch(token, @"^\d+$"))
        {
            if (int.TryParse(token, out var num))
                return $"RG{num:D8}";
        }

        return string.Empty;
    }

    private static string TryParseBookingId(string token)
    {
        if (token.StartsWith("BO", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("BK", StringComparison.OrdinalIgnoreCase))
        {
            var numberPart = token[2..];
            if (int.TryParse(numberPart, out var num))
                return $"{token[..2].ToUpper()}{num:D8}";
        }

        if (Regex.IsMatch(token, @"^\d+$"))
        {
            if (int.TryParse(token, out var num))
                return $"BO{num:D8}";
        }

        return string.Empty;
    }

    private static string TryParsePasienId(string token, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("PasienId prefix must not be empty", nameof(prefix));

        // Numeric only
        if (Regex.IsMatch(token, @"^\d+$"))
        {
            if (int.TryParse(token, out var num))
                return $"{prefix}{num:D8}";
        }

        // Pattern like "81-02-11"
        if (Regex.IsMatch(token, @"^\d{1,3}-\d{1,3}-\d{1,3}$"))
        {
            var digitsOnly = token.Replace("-", "");
            return $"{prefix}{digitsOnly.PadLeft(8, '0')}";
        }

        return string.Empty;
    }

    private static string TryParseDate(string token)
    {
        if (DateTime.TryParseExact(token, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyy-MM-dd");

        if (DateTime.TryParseExact(token, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsed))
            return parsed.ToString("yyyy-MM-dd");

        return string.Empty;
    }

    private static bool AllEmpty(params string[] values) => values.All(string.IsNullOrEmpty);

    private static string AppendText(string existing, string token)
        => string.IsNullOrEmpty(existing) ? token : $"{existing} {token}";
}