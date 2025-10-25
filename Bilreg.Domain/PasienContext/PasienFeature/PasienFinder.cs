using System.Globalization;
using System.Text.RegularExpressions;
using Bilreg.Domain.Helpers;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public record PasienFinder(
    string PasienId,
    string PasienName,
    string TglLahir,
    string IbuKandung,
    string Alamat,
    string RegId,
    string BookingId)
{
    public static PasienFinder CreateNew(string keyword, string pasienIdPrefix)
    {
        keyword = keyword.ToUpper();
        if (string.IsNullOrWhiteSpace(keyword))
            return new PasienFinder("", "", "", "", "", "", "");

        var tokens = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var pasienId = "";
        var regId = "";
        var bookingId = "";
        var tglLahir = "";
        var pasienName = "";
        var ibuKandung = "";
        var alamat = "";

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
            
            // Anything else: name, mother, address
            var normalize = token.NormalizeToEyd();
            pasienName = AppendText(pasienName, normalize);
            ibuKandung = AppendText(ibuKandung, normalize);
            alamat = AppendText(alamat, token);
        }

        return new PasienFinder(
            PasienId: pasienId.ToUpper(),
            PasienName: pasienName.ToUpper().Trim(),
            TglLahir: tglLahir,
            IbuKandung: ibuKandung.ToUpper().Trim(),
            Alamat: alamat.ToUpper().Trim(),
            RegId: regId,
            BookingId: bookingId
        );
    }

    // ========== PRIVATE HELPERS ==========

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