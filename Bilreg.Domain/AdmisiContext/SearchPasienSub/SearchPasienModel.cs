using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;
using System.Text.RegularExpressions;

namespace Bilreg.Domain.AdmisiContext.SearchPasienSub;

public record SearchPasienType : IPasienKey, IRegKey
{
    #region FACTORY
    public SearchPasienType(string pasienId, 
        string pasienName, 
        DateTime tglLahir, 
        GenderType gender, 
        IdentitasType identitas,
        string ibuKandung, 
        AlamatType alamat,
        string regId, 
        string bookingId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));

        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;
        Identitas = identitas;
        IbuKandung = ibuKandung;
        AlamatDomisili = alamat;
        RegId = regId;
        BookingId = bookingId;
    }

    public static SearchPasienType Default => new SearchPasienType(
        "-", "-", new DateTime(3000,1,1), GenderType.Default, IdentitasType.Default, 
        "-", AlamatType.Default, "-", "-");


    public static IEnumerable<SearchPasienType> Create(string keyword)
    {
        var pasien = SearchPasienType.Default;
        return keyword switch
        {
            var k when IsTglLahir(k) => new[] { ByTglLahir(pasien, k) },
            var k when IsRG(k) => new[] { ByRegId(pasien, k) },
            var k when IsBooking(k) => new[] { ByBooking(pasien, k) },
            var k when IsPasienId(k) => new[] { ByPasienId(pasien, k) },
            _ => ByName(pasien, keyword)
        };

    }

    #endregion

    #region PROPERTIES
    public string PasienId { get; init; }
    public string PasienName { get; init; }
    public DateTime TglLahir { get; init; }
    public GenderType Gender {  get; init; }
    public IdentitasType Identitas { get; init; }
    public string IbuKandung { get; init; }
    public AlamatType AlamatDomisili { get; init; }
    public  string RegId { get; init; }
    public string BookingId { get; init; }
    #endregion

    #region BEHAVIOR
    private static bool IsTglLahir(string keyword) =>
    DateTime.TryParseExact(keyword, "yyyy-MM-dd", null,
        System.Globalization.DateTimeStyles.None, out _);

    private static bool IsRG(string keyword) =>
        Regex.IsMatch(keyword, @"^RG\d+$", RegexOptions.IgnoreCase);

    private static bool IsPasienId(string keyword) =>
        keyword.All(char.IsDigit);

    private static bool IsBooking(string keyword) =>
    Regex.IsMatch(keyword, @"^(BH|BO)\d+$", RegexOptions.IgnoreCase);


    private static SearchPasienType ByPasienId(SearchPasienType pasien, string keyword) =>
        pasien with { PasienId = keyword };

    private static SearchPasienType ByTglLahir(SearchPasienType pasien, string keyword) =>
        pasien with { TglLahir = keyword.ToDate("yyyy-MM-dd") };
    private static SearchPasienType ByRegId(SearchPasienType pasien, string keyword) =>
         pasien with { RegId = keyword };
        
    private static SearchPasienType ByBooking(SearchPasienType pasien, string keyword) =>
        pasien with { BookingId = keyword };
    private static IEnumerable<SearchPasienType> ByName(SearchPasienType pasien, string keyword)
    {
        var varianNamas = GenerateVariasiEjaan(keyword);
        return varianNamas.Select(x => pasien with { PasienName = x });
    }


    private record Ejaan(string Eja1, string Eja2);
    private static IEnumerable<string> GenerateVariasiEjaan(string originName)
    {
        var spellingVariations = new List<Ejaan>
         {
             new("dj", "j"), new("j", "dj"),
             new("tj", "c"), new("c", "tj"),
             new("sj", "sy"), new("sy", "sj"),
             new("oe", "u"), new("u", "oe"),
             new("dh", "d"), new("d", "dh"),
             new("j", "y"), new("y", "j"),
             new("i", "ie"), new("ie", "i")
         };

        var cleanOriginName = RemovePunctuation(originName);

        // collection variasi ejaan original name
        var result = spellingVariations
            .Aggregate(new List<string> { cleanOriginName }, (variants, entry) => variants
                .Concat(variants
                    .Where(name => name.Contains(entry.Eja1, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name.Replace(entry.Eja1, entry.Eja2, StringComparison.OrdinalIgnoreCase))
                ).Distinct().ToList()
            );
        return result;
    }
    private static string RemovePunctuation(string input)
    {
        return new string(input.Where(c => !char.IsPunctuation(c)).ToArray());
    }



    #endregion

}
