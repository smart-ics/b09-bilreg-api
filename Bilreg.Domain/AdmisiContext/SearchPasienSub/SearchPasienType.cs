using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;
using System.Text.RegularExpressions;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.AdmisiContext.SearchPasienSub;

public record SearchPasienType : IPasienKey, IRegKey
{
    #region FACTORY
    public SearchPasienType(string pasienId, 
        string pasienName, 
        DateTime tglLahir, 
        string genderId,
        string genderName,
        IdentitasType identitas,
        string ibuKandung, 
        AlamatType alamat,
        string regId, 
        string bookingId)
    {

        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        GenderId = genderId;
        GenderName = genderName;
        Identitas = identitas;
        IbuKandung = ibuKandung;
        AlamatDomisili = alamat;
        RegId = regId;
        BookingId = bookingId;
    }

    public static SearchPasienType Default => new SearchPasienType(
        "-", "-", new DateTime(3000,1,1), "-", "-", IdentitasType.Default, 
        "-", AlamatType.Default, "-", "-");


    public static SearchPasienType CreateNotName(SearchPasienType pasien, string keyword)
    {
        return keyword switch
        {
            _ when IsTglLahir(keyword) => ByTglLahir(pasien, keyword) ,
            _ when IsRG(keyword) => ByRegId(pasien, keyword) ,
            _ when IsBooking(keyword) => ByBooking(pasien, keyword),
            _ when IsPasienId(keyword) =>  ByPasienId(pasien, keyword),
            _ => throw new ArgumentOutOfRangeException(nameof(keyword), keyword, null)
        };

    }

    #endregion

    #region PROPERTIES
    public string PasienId { get; init; }
    public string PasienName { get; init; }
    public DateTime TglLahir { get; private set; }
    public string GenderId { get; set; }
    public string GenderName { get; set; }
    public IdentitasType Identitas { get; init; }
    public string IbuKandung { get; init; }
    public AlamatType AlamatDomisili { get; init; }
    public  string RegId { get; private set; }
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

    private static bool IsPasienName(string keyword) =>
        Regex.IsMatch(keyword, @"^[A-Za-z\s]+$");

    private static SearchPasienType ByPasienId(SearchPasienType pasien, string keyword) =>
        pasien with { PasienId = keyword };

    private static SearchPasienType ByTglLahir(SearchPasienType pasien, string keyword) =>
        pasien with { TglLahir = keyword.ToDate("yyyy-MM-dd") };

    private static SearchPasienType ByRegId(SearchPasienType pasien, string keyword) =>
         pasien with { RegId = keyword };
        
    private static SearchPasienType ByBooking(SearchPasienType pasien, string keyword) =>
        pasien with { BookingId = keyword };
    
    
    public static IEnumerable<SearchPasienType> GenData(List<string> parts)
    {
        var partName = parts.FirstOrDefault(IsPasienName);
        var hasName = !string.IsNullOrWhiteSpace(partName);

        if (hasName)
            parts.Remove(partName);

        var result = new List<SearchPasienType>();

        if (hasName)
        {
            // jika ada nama -> buat varian ejaan
            var varianNamas = GenerateVariasiEjaan(partName!);
            result = varianNamas
                .Select(x => SearchPasienType.Default with { PasienName = x })
                .ToList();
        }
        else
        {
            // jika tidak ada nama -> mulai dari 1 default saja
            result.Add(SearchPasienType.Default);
        }

        // proses bagian lain (tgl lahir, rg, booking, dll)
        foreach (var part in parts)
        {
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = CreateNotName(result[i], part);
            }
        }

        return result;


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

    #region CHECKERS
    public bool HasPasienId => !string.IsNullOrWhiteSpace(PasienId) && PasienId != "-";
    public bool HasPasienName => !string.IsNullOrWhiteSpace(PasienName) && PasienName != "-";
    public bool HasTglLahir => TglLahir != default && TglLahir != new DateTime(3000, 1, 1);
    public bool HasGender => !string.IsNullOrEmpty(GenderId);
    public bool HasIdentitas => Identitas != null && Identitas != IdentitasType.Default;
    public bool HasIbuKandung => !string.IsNullOrWhiteSpace(IbuKandung) && IbuKandung != "-";
    public bool HasAlamat => AlamatDomisili != null && AlamatDomisili != AlamatType.Default;
    public bool HasRegId => !string.IsNullOrWhiteSpace(RegId) && RegId != "-";
    public bool HasBookingId => !string.IsNullOrWhiteSpace(BookingId) && BookingId != "-";
    #endregion

    #endregion

}
