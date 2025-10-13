using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System.Xml.Serialization;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record QuickSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienType>>;

public class QuickSearchPasienHandler : IRequestHandler<QuickSearchPasienQuery, IEnumerable<SearchPasienType>>
{
    private readonly IQuickSearchPasienDal _quickSearchPasienDal;

    public QuickSearchPasienHandler(IQuickSearchPasienDal quickSearchPasienDal)
    {
        _quickSearchPasienDal = quickSearchPasienDal;
    }

    public Task<IEnumerable<SearchPasienType>> Handle(QuickSearchPasienQuery request, CancellationToken cancellationToken)
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

    private Func<IEnumerable<SearchPasienType>, string, IEnumerable<SearchPasienType>> DetermineSearchStrategy(string keyword)
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
    private IEnumerable<SearchPasienType> SearchByTglLahir(IEnumerable<SearchPasienType> listData, string tgllahir)
    {
        var result = listData
            .Where(x => x.TglLahir.ToString("yyyy-MM-dd") == tgllahir)
            ?.ToList() ?? new List<SearchPasienType>();
        return result;
    }
    #endregion
    #region ByPasienId
    private IEnumerable<SearchPasienType> SearchByPasienId(IEnumerable<SearchPasienType> listData, string pasienId)
    {
        var result = listData
            .Where(x => x.PasienId == pasienId)
            ?.ToList() ?? new List<SearchPasienType>();
        return result;
    }
    #endregion
    #region ByRegId
    private IEnumerable<SearchPasienType> SearchByRegId(IEnumerable<SearchPasienType> listData, string regId)
    {
        var result = listData
            .Where(x => x.RegId == regId)
            ?.ToList() ?? new List<SearchPasienType>();
        return result;
    }
    #endregion
    #region ByNik
    private IEnumerable<SearchPasienType> SearchByNik(IEnumerable<SearchPasienType> listData, string nik)
    {
        var result = listData
            .Where(x => x.Identitas.JenisId.Equals("KTP") && x.Identitas.NomorId == nik)
            ?.ToList() ?? new List<SearchPasienType>();
        return result;
    }
    #endregion
    #region ByName
    private IEnumerable<SearchPasienType> SearchByPasienName(IEnumerable<SearchPasienType> listData, string name)
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
    
    private static IEnumerable<SearchPasienType> FindSimilarity(IEnumerable<SearchPasienType> listData,
        string keyword)
    {
        var listPasienJaroWinkler = listData.Select(x => new
        {
            Pasien = x,
            JaroWinklerValue = x.PasienName.Similiarity(keyword)
        });
        var result = listPasienJaroWinkler
            .Where(x => x.JaroWinklerValue >= 0.75)
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

    private static IEnumerable<SearchPasienType> FindByWords(IEnumerable<SearchPasienType> listData,
        IEnumerable<string> variasiEjaan, 
        string searchKeyword)
    {
        var variasiWordCount = searchKeyword.Split(' ').Length;
        var wordCountMin = Math.Min(variasiWordCount, 2);
        var result = new List<SearchPasienType>();
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

public class QuickSearchPasienTest
{
    private readonly QuickSearchPasienHandler _sut;
    private readonly Mock<IQuickSearchPasienDal> _dal;

    public QuickSearchPasienTest()
    {
        _dal = new Mock<IQuickSearchPasienDal>();
        _sut = new QuickSearchPasienHandler(_dal.Object);
    }

    [Fact]
    public async Task T01_GivenValidName_WhenQuickSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Andi", new DateTime(2001, 09,13), GenderType.Default, 
            IdentitasType.Default, "-",AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Budi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };
        
        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));
        
        var request = new QuickSearchPasienQuery("Andi");
        
        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienId.Should().Be("A");
    }

    [Fact]
    public async Task T02_GivenValidName_WhenQuickSearch_ThenReturnListSimilarPasienName()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 09, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 02, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new QuickSearchPasienQuery("hardi");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public async Task T03_GivenValidTglLahir_WhenQuickSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 9, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 8, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 2, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2, faker3 };

        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new QuickSearchPasienQuery("1999-02-19");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienId.Should().Be("C");
    }

    [Fact]
    public async Task T04_GivenValidPasienId_WhenQuickSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("121", "Andi", new DateTime(2001, 09, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("122", "Budi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new QuickSearchPasienQuery("122");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienName.Should().Be("Budi");
    }

    [Fact]
    public async Task T05_GivenValidRegId_WhenQuickSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("121", "Andi", new DateTime(2001, 09, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "RG001", "-");
        var faker2 = new SearchPasienType("122", "Budi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "RG002", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new QuickSearchPasienQuery("RG001");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienName.Should().Be("Andi");
    }
}
