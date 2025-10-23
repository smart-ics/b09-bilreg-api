using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System.Text.RegularExpressions;
using Xunit;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record DeepSearchPasienQuery(string Keyword) : IRequest<IEnumerable<DeepSearchPasienResponse>>;

public record DeepSearchPasienResponse(
        string PasienId,
        string PasienName,
        string TglLahir,
        string Gender,
        IdentitasType Identitas,
        string IbuKandung,
        AlamatType Alamat,
        string RegId,
        string BookingId);
public class DeepSearchPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<DeepSearchPasienResponse>>
{
    private readonly IDeepSearchPasienDal _deepSearchDal;

    public DeepSearchPasienHandler(IDeepSearchPasienDal deepSearchDal)
    {
        _deepSearchDal = deepSearchDal;
    }

    public Task<IEnumerable<DeepSearchPasienResponse>> Handle(DeepSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");
        
        var parts = request.Keyword
            .Split(';')
            .Select(x => x.ToLower())
            .Select(x => x.Trim())
            .ToList();

        var dataSearch = SearchPasienType.GenData(parts);

        var dataResult = _deepSearchDal.ListData(dataSearch)
            .Match(
                some => some,
                () => new List<SearchPasienType>()
            );
        var result = dataResult.Select(x => new DeepSearchPasienResponse(
                x.PasienId, x.PasienName, x.TglLahir.ToString("yyyy-MM-dd"),
                x.Gender, x.Identitas, x.IbuKandung, x.AlamatDomisili, x.RegId, x.BookingId
            )).OrderBy(x => x.PasienName);

        return Task.FromResult(result.Distinct());
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
        var faker1 = new SearchPasienType("A", "Andi", new DateTime(2001, 09, 13), "P",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Andhi", new DateTime(2000, 08, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2 };

        _dal.Setup(x => x.ListData(It.IsAny<IEnumerable<SearchPasienType>>()))
            .Returns(MayBe.From<IEnumerable<SearchPasienType>>(listFacker));

        var request = new DeepSearchPasienQuery("Andi");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.First().PasienId.Should().Be("B");
    }

    [Fact]
    public async Task T02_GivenValidName_WhenDeepSearch_ThenReturnListSimilarPasienName()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 09, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 08, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 02, 19), "-",
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

    [Fact]
    public async Task T03_GivenValidTglLahir_WhenDeepSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 9, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2000, 8, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 2, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2, faker3 };

        _dal.Setup(x => x.ListData(It.IsAny<IEnumerable<SearchPasienType>>()))
            .Returns<IEnumerable<SearchPasienType>>(searchParams =>
            {
                var tanggalDicari = searchParams.First().TglLahir;
                var hasil = listFacker.Where(x => x.TglLahir == tanggalDicari);
                return MayBe.From<IEnumerable<SearchPasienType>>(hasil);
            });

        var request = new DeepSearchPasienQuery("1999-02-19");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("C");
    }

    [Fact]
    public async Task T04_GivenValidNameAndTglLahir_WhenDeepSearch_ThenReturnPasien()
    {
        // ARRANGE
        var faker1 = new SearchPasienType("A", "Suhardi Wijaya", new DateTime(2001, 9, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker2 = new SearchPasienType("B", "Soehardi", new DateTime(2001, 9, 13), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var faker3 = new SearchPasienType("C", "Agus", new DateTime(1999, 2, 19), "-",
            IdentitasType.Default, "-", AlamatType.Default, "-", "-");
        var listFacker = new List<SearchPasienType> { faker1, faker2, faker3 };

        _dal.Setup(x => x.ListData(It.IsAny<IEnumerable<SearchPasienType>>()))
            .Returns<IEnumerable<SearchPasienType>>(searchParams =>
            {
                // Ambil yang punya nama + tgl lahir
                var first = searchParams.First();
                var tanggalDicari = first.TglLahir;
                var namaVariasi = searchParams
                    .Where(x => x.HasPasienName)
                    .Select(x => x.PasienName.ToLower())
                    .ToList();

                var hasil = listFacker.Where(x =>
                    x.TglLahir == tanggalDicari &&
                    namaVariasi.Any(v => x.PasienName.ToLower().Contains(v))
                );

                return MayBe.From<IEnumerable<SearchPasienType>>(hasil);
            });

        var request = new DeepSearchPasienQuery("Suhardi; 2001-09-13");

        // ACT
        var response = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A", "B");
    }
}