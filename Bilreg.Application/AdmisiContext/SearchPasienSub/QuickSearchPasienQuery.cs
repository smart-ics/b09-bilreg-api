using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record QuickSearchPasienQuery(string Keyword) : IRequest<IEnumerable<QuickSearchPasienResponse>>;

public record QuickSearchPasienResponse(
        string PasienId,
        string PasienName,
        string TglLahir,
        string Gender,
        IdentitasType Identitas,
        string IbuKandung,
        AlamatType Alamat,
        string RegId,
        string BookingId);

public class QuickSearchPasienHandler : IRequestHandler<QuickSearchPasienQuery, IEnumerable<QuickSearchPasienResponse>>
{
    private readonly IQuickSearchPasienDal _quickSearchPasienDal;

    public QuickSearchPasienHandler(IQuickSearchPasienDal quickSearchPasienDal)
    {
        _quickSearchPasienDal = quickSearchPasienDal;
    }

    public Task<IEnumerable<QuickSearchPasienResponse>> Handle(QuickSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");

        var tgl = DateTime.Now;
        var periode = new Periode(tgl);
        var keyword = request.Keyword.Trim();

        var parts = keyword
            .Split(';')
            .Select(x => x.ToLower())
            .Select(x => x.Trim())
            .ToList();

        var dataSearch = SearchPasienType.GenData(parts);

        var listData = _quickSearchPasienDal.ListData(periode)
            .Match(
                some => some,
                () => throw new ArgumentException("data not found")
            );

        var dataResult = SearchData(listData, dataSearch).Any()
            ? SearchData(listData, dataSearch)
            : new List<SearchPasienType>();

        var result = dataResult.Select(x => new QuickSearchPasienResponse(
                x.PasienId, x.PasienName, x.TglLahir.ToString("yyyy-MM-dd"), 
                x.Gender, x.Identitas, x.IbuKandung, x.AlamatDomisili, x.RegId, x.BookingId
            )).OrderBy(x => x.PasienName);

        return Task.FromResult(result.Distinct());

    }


    private IEnumerable<SearchPasienType> SearchData(IEnumerable<SearchPasienType> listDataPasien, IEnumerable<SearchPasienType> dataSearch)
    {
        var result = listDataPasien
            .Where(dp => dataSearch.Any(sp =>
                (
                    // PasienId
                    sp.HasPasienId && dp.PasienId.Contains(sp.PasienId)
                )
                ||
                (
                    // PasienName + TglLahir
                    sp.HasPasienName && sp.HasTglLahir &&
                    dp.TglLahir == sp.TglLahir &&
                    dp.PasienName.ToLower().Contains(sp.PasienName.ToLower())
                )
                ||
                (
                    // PasienName 
                    sp.HasPasienName && !sp.HasTglLahir &&
                    dp.PasienName.ToLower().Contains(sp.PasienName.ToLower())
                )
                ||
                (
                    // TglLahir
                    sp.HasTglLahir && dp.TglLahir == sp.TglLahir
                )
                ||
                (
                    // RegId
                    sp.HasRegId && dp.RegId.ToLower() == sp.RegId.ToLower()
                )
                ||
                (
                    // BookingId
                    sp.HasBookingId && dp.BookingId.ToLower() == sp.BookingId.ToLower()
                )
            ))
            .ToList() ?? new List<SearchPasienType>();
        return result;
    }
    
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
        var faker1 = new SearchPasienType("A", "Andi", new DateTime(2001, 09,13), "-", 
            IdentitasType.Default, "-",AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Budi", new DateTime(2000, 08, 19), "-",
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
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 09, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 08, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 02, 19), "-",
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
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 9, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 8, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 2, 19), "-",
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
        var faker1 = new SearchPasienType("121", "Andi", new DateTime(2001, 09, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("122", "Budi", new DateTime(2000, 08, 19), "-",
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
        var faker1 = new SearchPasienType("121", "Andi", new DateTime(2001, 09, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "RG001", "-");
        var faker2 = new SearchPasienType("122", "Budi", new DateTime(2000, 08, 19), "-",
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

    [Fact]
    public async Task T06_GivenKeywordNamaAndTgllahir_WhenQuickSearch_ThenReturnpasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("121", "Andi", new DateTime(2001, 09, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "RG001", "-");
        var faker2 = new SearchPasienType("122", "Budi", new DateTime(2000, 08, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "RG002", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new QuickSearchPasienQuery("Andi; 2001-09-13");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienId.Should().Be("121");
    }

}
