using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienFindFast(string PasienId) : IRequest<IEnumerable<PasienFindFastResponse>>, IPasienKey;

public record PasienFindFastResponse(
    string PasienId,
    string PasienName,
    string TglLahir,
    string TglMedrec
);

public class PasienFindFastHandler : IRequestHandler<PasienFindFast, IEnumerable<PasienFindFastResponse>>
{
    private readonly IPasienDal _pasienDal;


    public PasienFindFastHandler(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;

    }
    
    public async Task<IEnumerable<PasienFindFastResponse>> Handle(PasienFindFast request, CancellationToken cancellationToken)
    {
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id :{request.PasienId} not found");
        
        var listPasien = _pasienDal.ListData(pasien.TglLahir)?.ToList()
            ?? throw new KeyNotFoundException($"Data Pasien not Found");
        
        var resultJw = FindSimiliarity(listPasien, pasien.PasienName);
        var resultEjaan = FindEjaanLamaBaru(listPasien, pasien.PasienName);

        var result = resultJw.Union(resultEjaan);
        
        var response = result.Select(BuildPasienResponse);
        return await Task.FromResult(response);
    }

    private static IEnumerable<PasienModel> FindEjaanLamaBaru(
        List<PasienModel> listPasien, string basedName)
    {
        var spellingVariations = new Dictionary<string, string>
        {
            { "dj", "j" },
            { "tj", "c" },
            { "sj", "sy" },
            { "oe", "u" },
            { "dh", "d" },
            { "j", "y" }
        };

        var basedVariationNames = spellingVariations
            .Aggregate(new List<string> { basedName }, (variants, entry) => variants
                .Concat(variants
                    .Where(name => name.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name.Replace(entry.Key, entry.Value, StringComparison.OrdinalIgnoreCase))
                ).ToList()
            );
        
        var result = listPasien.Where(pasien =>
        {
            var pasienVariants = spellingVariations
                .Aggregate( new List<string> { pasien.PasienName }, (variants, entry) => variants
                    .Concat(variants
                        .Where(name => name.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(name => name.Replace(entry.Key, entry.Value, StringComparison.OrdinalIgnoreCase))
                ).ToList()
            );
        
            return basedVariationNames.Any(variant =>
                pasienVariants.Any(pasienVariant =>
                    pasienVariant.Equals(variant, StringComparison.OrdinalIgnoreCase)
                )
            );
        });

        return result;
    }


    private static IEnumerable<PasienModel> FindSimiliarity(
        IEnumerable<PasienModel> listPasien, string name)
    {
        //  hitung nilai Jaro Winkler Value
        var listPasienJwValue = listPasien.Select(x => new
        {
            Pasien = x,
            JaroWinklerValue = x.PasienName.Similiarity(name)
        });
        //  filter yang lebih dari 0.75
        var result = listPasienJwValue
            .Where(x => x.JaroWinklerValue >= 0.75)
            .Select(x => x.Pasien);
        return result;
    }

    private PasienFindFastResponse BuildPasienResponse(PasienModel pasien)
    {
        // Return respon sementara
        return new PasienFindFastResponse(
            pasien.PasienId,
            pasien.PasienName,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.TglMedrec.ToString(DateFormatEnum.YMD)
        );
    }
}

public class PasienFindFastTest
{
    private readonly PasienFindFastHandler _sut;
    private readonly Mock<IPasienDal> _pasienDal;

    public PasienFindFastTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _sut = new PasienFindFastHandler(_pasienDal.Object);
    }

    [Fact]
    public async Task GivenSimiliarPasienName_ThenReturnThePasien()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Alexander Suryaputra");
        var faker2 = new PasienModel("B", "Alexandria  Suryaputra");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }

    [Fact]
    public async Task GivenNotSimiliarPasienName_ThenReturnThePasien()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Cahyadi");
        var faker2 = new PasienModel("B", "Alexandria  Suryaputra");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenEjaanLamaPasienName_ThenReturnThePasienWithEjaanBaru()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Subur Husnuloh");
        var faker2 = new PasienModel("B", "Boedi Soeboer Hoesnoeloh");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }
    
    [Fact]
    public async Task GivenEjaanBaruPasienName_ThenReturnThePasienWithEjaanLama()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Boedi Soeboer Hoesnoeloh");
        var faker2 = new PasienModel("B", "Budi Subur Husnuloh");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }
    
    [Fact]
    public async Task GivenNamaSamaSebagian_ThenReturnThePasienWithEjaanBaru()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Subur Husnuloh");
        var faker2 = new PasienModel("B", "Boedi Soeboer Hoesnoeloh");
        var faker1A = new PasienModel("C", "Sunardinata");
        var faker1B = new PasienModel("D", "Budi Subur");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1, faker1A, faker1B});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Should().ContainEquivalentOf(faker1);
        response.Should().ContainEquivalentOf(faker1A);
    }    

    [Fact]
    public async Task GivenSoekarno_ThenReturnSukarto_BecauseJaroWinkler_NotEjaan()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Sukarno");
        var faker2 = new PasienModel("B", "Soekarno");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("A");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }

}

