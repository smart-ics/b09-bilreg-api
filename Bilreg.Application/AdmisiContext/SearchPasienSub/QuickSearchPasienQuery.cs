using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record QuickSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienModel>>;

public class QuickSearchPasienHandle : IRequestHandler<QuickSearchPasienQuery, IEnumerable<SearchPasienModel>>
{
    private readonly IQuickSearchPasienDal _quickSearchPasienDal;

    public QuickSearchPasienHandle(IQuickSearchPasienDal quickSearchPasienDal)
    {
        _quickSearchPasienDal = quickSearchPasienDal;
    }

    public Task<IEnumerable<SearchPasienModel>> Handle(QuickSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");

        var tgl = DateTime.Now;
        var periode = new Periode(tgl);
        var keyword = request.Keyword.Trim();

        var listData = _quickSearchPasienDal.ListData(periode)
            .Match(
                some => some,
                () => throw new InvalidOperationException("data not found")
            );

        var result = DetermineSearchStrategy(keyword)(listData, keyword);

        return Task.FromResult(result.Distinct());

    }

    private Func<IEnumerable<SearchPasienModel>, string, IEnumerable<SearchPasienModel>> DetermineSearchStrategy(string keyword)
    => keyword switch
    {
        _ when IsTanggal(keyword) => SearchByTglLahir,
        _ when IsNumeric(keyword) => (data, key) => SearchByPasienId(data, key)
                                        .Concat(SearchByNik(data, keyword)),
        _ => (data, key) => SearchByPasienName(data, key)
                                        .Concat(SearchByRegId(data, key))
    };

    #region Private_Helper
    private static bool IsTanggal(string keyword)
        => DateTime.TryParseExact(keyword, "yyyy-MM-dd", null, 
            System.Globalization.DateTimeStyles.None, out _);

    private static bool IsNumeric(string keyword)
        => keyword.All(char.IsDigit);
    #region ByTgllahir
    private IEnumerable<SearchPasienModel> SearchByTglLahir(IEnumerable<SearchPasienModel> listData, string tgllahir)
    {
        var result = listData
            .Where(x => x.TglLahir.ToString("yyyy-MM-dd") == tgllahir)
            ?.ToList() ?? new List<SearchPasienModel>();
        return result;
    }
    #endregion
    #region ByPasienId
    private IEnumerable<SearchPasienModel> SearchByPasienId(IEnumerable<SearchPasienModel> listData, string pasienId)
    {
        var result = listData
            .Where(x => x.PasienId == pasienId)
            ?.ToList() ?? new List<SearchPasienModel>();
        return result;
    }
    #endregion
    #region ByRegId
    private IEnumerable<SearchPasienModel> SearchByRegId(IEnumerable<SearchPasienModel> listData, string regId)
    {
        var result = listData
            .Where(x => x.RegId == regId)
            ?.ToList() ?? new List<SearchPasienModel>();
        return result;
    }
    #endregion
    #region ByNik
    private IEnumerable<SearchPasienModel> SearchByNik(IEnumerable<SearchPasienModel> listData, string nik)
    {
        var result = listData
            .Where(x => x.Identitas.JenisId.Equals("KTP") && x.Identitas.NomorId == nik)
            ?.ToList() ?? new List<SearchPasienModel>();
        return result;
    }
    #endregion
    #region ByName
    private IEnumerable<SearchPasienModel> SearchByPasienName(IEnumerable<SearchPasienModel> listData, string name)
    {
        var similarPasienName = FindSimilarity(listData, name);
        var variasiEjaan = GenerateVariasiEjaan(name);
        var distinctWords = variasiEjaan
            .SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resultEjaanByWords = FindByWords(listData, distinctWords, name);
        var result = similarPasienName.Union(resultEjaanByWords);

        return result;
    }
    
    private static IEnumerable<SearchPasienModel> FindSimilarity(IEnumerable<SearchPasienModel> listData,
        string keyword)
    {
        var listPasienJaroWinkler = listData.Select(x => new
        {
            Pasien = x,
            JaroWinklerValue = x.PasienName.Similiarity(keyword)
        });
        var result = listPasienJaroWinkler
            .Where(x => x.JaroWinklerValue >= 0.85)
            .Select(x => x.Pasien);
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

    private static IEnumerable<SearchPasienModel> FindByWords(IEnumerable<SearchPasienModel> listData,
        IEnumerable<string> variasiEjaan, 
        string searchKeyword)
    {
        var variasiWordCount = searchKeyword.Split(' ').Length;
        var wordCountMin = Math.Min(variasiWordCount, 2);
        var result = new List<SearchPasienModel>();
        foreach (var pasien in listData)
        {
            var pasienNameClean = RemovePunctuation(pasien.PasienName);
            var listWords = pasienNameClean.Split(' ');
            var found = listWords
                .Count(wordPasien => variasiEjaan
                    .Any(item => item
                        .Equals(wordPasien, StringComparison.CurrentCultureIgnoreCase)));
            if (found >= wordCountMin)
                result.Add(pasien);
        }
        return result;
    }
    #endregion
    
    #endregion
}
