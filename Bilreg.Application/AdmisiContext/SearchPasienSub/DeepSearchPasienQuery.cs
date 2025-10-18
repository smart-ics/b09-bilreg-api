using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record DeepSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienType>>;

public class DeepSearchPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<SearchPasienType>>
{
    private readonly IDeepSearchPasienDal _deepSearchDal;

    public DeepSearchPasienHandler(IDeepSearchPasienDal deepSearchDal)
    {
        _deepSearchDal = deepSearchDal;
    }

    public Task<IEnumerable<SearchPasienType>> Handle(DeepSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");
        
        var parts = request.Keyword
            .Split(';')
            .Select(x => x.Trim())
            .ToArray();

        var allResults = new List<SearchPasienType>();

        foreach (var part in parts)
        {
            var searchTypes = SearchPasienType.Create(part);

            var result = _deepSearchDal.ListData(searchTypes)
                .Match(
                    some => some,
                    () => new List<SearchPasienType>()
                );

            allResults.AddRange(result);
        }

        return Task.FromResult(allResults.Distinct());
    }

    
}


public class DeepSearchPasienTest
{
    private readonly DeepSearchPasienHandler _sut;
    private readonly Mock<IDeepSearchPasienDal> _dal;

    public DeepSearchPasienTest()
    {
        _dal = new Mock<IDeepSearchPasienDal>();
        _sut = new DeepSearchPasienHandler(_dal.Object);
    }

    [Fact]
    public async Task T01_GivenValidName_WhenDeepSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Andi", new DateTime(2001, 09, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Andhi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<IEnumerable<SearchPasienType>>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new DeepSearchPasienQuery("Andi");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienId.Should().Be("A");
    }

    [Fact]
    public async Task T02_GivenValidName_WhenDeepSearch_ThenReturnListSimilarPasienName()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 09, 13), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 08, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 02, 19), GenderType.Default,
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<IEnumerable<SearchPasienType>>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new DeepSearchPasienQuery("hardi");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A", "B");
    }

    //[Fact]
    //public async Task T03_GivenValidTglLahir_WhenDeepSearch_ThenReturnPasien()
    //{
    //    // ARRANGE
    //    var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 9, 13), GenderType.Default,
    //        IdentitasType.Default, "-", AlamatType.Default, "-", "-");
    //    var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 8, 19), GenderType.Default,
    //        IdentitasType.Default, "-", AlamatType.Default, "-", "-");
    //    var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 2, 19), GenderType.Default,
    //        IdentitasType.Default, "-", AlamatType.Default, "-", "-");
    //    var listFacker = new List<SearchPasienType> { faker1, faker2, faker3 };

    //    _dal.Setup(x => x.ListData(It.IsAny<string>()))
    //        .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

    //    var request = new DeepSearchPasienQuery("1999-02-19");

    //    // ACT
    //    var response = await _sut.Handle(request, CancellationToken.None);

    //    // ASSERT
    //    response.First().PasienId.Should().Be("C");
    //}

}