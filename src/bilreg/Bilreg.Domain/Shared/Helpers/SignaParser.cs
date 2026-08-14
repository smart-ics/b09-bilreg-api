using System.Globalization;
using System.Text.RegularExpressions;

namespace Bilreg.Domain.Shared.Helpers;

public class SignaParser
{
    public static SignaType Parse(string prescriptionText)
    {
        /*
        Cara kerja utama
        1. Tentukan Anchor Symbol: 'dd' atau 'x'
        2. Ambil angka sebelum anchor sebagai DailyDose (DD)
        3. Ambil angka setelah anchor sebagai ConsumeAmount (CA)
        4. Antara Anchor dengan DD atau CA boleh ada spasi atau langsung
          
        Beberapa kasus variasi
        1. CA bisa ditulis dengan fraction
           Conntoh: "2dd 1/2" => DD = 2, CA = 0.5
                    "2dd 1/3" => DD = 2, CA = 0.33     
        2. CA bisa ditulis dengan decimal koma atau titik
           Contoh: "2dd 0.5" => DD = 2, CA = 0.5
                   "2dd 0,5" => DD = 2, CA = 0.5
        3. Jika tidak disebutkan CA, maka default-nya adalah 1
           Contoh: "2dd" => DD = 2, CA = 1
        */

        var normalizedText = NormalizeText(prescriptionText);
        SignaType result;
        
        result = ParsingVariant1_Fraction(normalizedText);
        if (result != null) return result;

        result = ParsingVariant2_Decimal(normalizedText);
        if (result != null) return result;

        result = ParsingNormalCase(normalizedText);
        if (result != null) return result;

        result = ParsingVariant3_NoConsumeAmount(normalizedText);
        if (result != null) return result;
        
        result = ParsingVariant4_DashPattern(normalizedText);
        if (result != null) return result;
        
        // If no match found, throw an exception
        throw new FormatException($"Invalid signa: '{prescriptionText}'");
    }

    private static string NormalizeText(string text)
    {
        var normalizedText = text.ToLower();
        normalizedText = Regex.Replace(normalizedText, @"(\d+)\s*x\s*", "$1 dd ");
        normalizedText = Regex.Replace(
            normalizedText,
            @"\b(sehari|tablet|tab|kapsul|sebelum makan|sesudah makan)\b",
            "",
            RegexOptions.IgnoreCase
        );
        normalizedText = Regex.Replace(normalizedText, @"\s+", " ").Trim();
        return normalizedText;
    }

    private static SignaType ParsingNormalCase(string text)
    {
        var wholeNumberSignaRegex = new Regex(@"(\d+)\s*dd\s*(\d+)");
        var match = wholeNumberSignaRegex.Match(text);

        if (!match.Success) return null;

        var dailyDose = int.Parse(match.Groups[1].Value);
        var consumeAmount = int.Parse(match.Groups[2].Value);

        return new SignaType
        {
            DailyDose = dailyDose,
            ConsumeAmount = consumeAmount
        };
    }
    
    private static SignaType ParsingVariant1_Fraction(string text)
    {
        var fractionSignaRegex = new Regex(@"(\d+)\s*dd\s*(\d+)/(\d+)");
        var match = fractionSignaRegex.Match(text);

        if (!match.Success) return null;
        
        var dailyDose = int.Parse(match.Groups[1].Value);
        var numerator = int.Parse(match.Groups[2].Value);
        var denominator = int.Parse(match.Groups[3].Value);
            
        var consumeAmount = (decimal)numerator / (decimal)denominator;
            
        return new SignaType
        {
            DailyDose = dailyDose,
            ConsumeAmount = consumeAmount
        };

    }
    
    private static SignaType ParsingVariant2_Decimal(string text)
    {
        var decimalSignaRegex = new Regex(@"(\d+)\s*dd\s*(\d+[.,]\d+)");
        var match = decimalSignaRegex.Match(text);

        if (!match.Success) return null;
        
        var dailyDose = int.Parse(match.Groups[1].Value);
            
        // Handle comma dan titik sbg decimal separators
        var consumeAmountStr = match.Groups[2].Value.Replace(',', '.');
        var consumeAmount = decimal.Parse(consumeAmountStr, CultureInfo.InvariantCulture);
            
        return new SignaType
        {
            DailyDose = dailyDose,
            ConsumeAmount = consumeAmount
        };
    }
    
    private static SignaType ParsingVariant3_NoConsumeAmount(string text)
    {
        var noConsumeSignaRegex = new Regex(@"(\d+)\s*dd(?!\s*\d)");
        var match = noConsumeSignaRegex.Match(text);

        if (!match.Success) return null;
        
        var dailyDose = int.Parse(match.Groups[1].Value);
            
        return new SignaType
        {
            DailyDose = dailyDose,
            ConsumeAmount = 1
        };
    }
    
    private static SignaType ParsingVariant4_DashPattern(string text)
    {
        var dashSignaRegex = new Regex(@"([0-9/.,]+(?:-[0-9/.,]+)+)");
        var match = dashSignaRegex.Match(text);
        
        if (!match.Success) return null;
        
        var pattern = match.Groups[1].Value;
        var parts = pattern.Split('-');
        var doses = parts.Select(ParseDose).Where(value => value != 0).ToList();
        
        if (doses.Count == 0)
            return null;
        
        var consumeAmount = doses[0];

        return new SignaType
        {
            DailyDose = doses.Count,
            ConsumeAmount = consumeAmount
        };
    }
    
    private static decimal ParseDose(string part)
    {
        part = part.Trim().Replace(",", ".");
        if (string.IsNullOrWhiteSpace(part)) return 0;

        if (!part.Contains('/')) return decimal.Parse(part, CultureInfo.InvariantCulture);
        
        var fraction = part.Split('/');
        return decimal.Parse(fraction[0], CultureInfo.InvariantCulture) 
               / decimal.Parse(fraction[1], CultureInfo.InvariantCulture);
    }
}

public class SignaType
{
    public int DailyDose { get; set; }
    public decimal ConsumeAmount { get; set; }
    public string FormattedSigna => $"{DailyDose} dd {ConsumeAmount}";

    public override string ToString()
    {
        return $"Signa Result = {FormattedSigna}\nDailyDose = {DailyDose}\nConsumeAmount = {ConsumeAmount}";
    }
}