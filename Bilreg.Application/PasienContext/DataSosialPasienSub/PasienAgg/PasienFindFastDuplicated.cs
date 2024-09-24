using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienFindFastDuplicated(string PasienId) : IRequest<IEnumerable<PasienFindFastDuplicatedResponse>>, IPasienKey;

public record PasienFindFastDuplicatedResponse(
    string PasienId,
    string PasienName,
    string TglLahir,
    string TglMedrec
);

public class PasienFindDuplicatedHandler : IRequestHandler<PasienFindFastDuplicated, IEnumerable<PasienFindFastDuplicatedResponse>>
{
    private IPasienDal _pasienDal;


    public PasienFindDuplicatedHandler(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;

    }
    
    public async Task<IEnumerable<PasienFindFastDuplicatedResponse>> Handle(PasienFindFastDuplicated request, CancellationToken cancellationToken)
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
        IEnumerable<PasienModel> listPasien, string pasienPasienName)
    {
        var spellingVariations = new Dictionary<string, string>
        {
            { "Dj", "J"}, 
            { "Tj", "C"}, 
            { "Sj", "Sy"},
            { "Oe", "U"},
            { "Dh", "D"},
            { "J", "Y" }
        };

        var possibleNameVariants = spellingVariations.Aggregate(
            new List<string> { pasienPasienName },
            (variants, entry) => variants.Concat(
                variants.Where(name => name.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name.Replace(entry.Key, entry.Value, StringComparison.OrdinalIgnoreCase))
            ).ToList()
        );

        var result = listPasien.Where(pasien =>
        {
            var pasienVariants = spellingVariations.Aggregate(
                new List<string> { pasien.PasienName },
                (variants, entry) => variants.Concat(
                    variants.Where(name => name.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(name => name.Replace(entry.Key, entry.Value, StringComparison.OrdinalIgnoreCase))
                ).ToList()
            );

            return possibleNameVariants.Any(variant =>
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

    private PasienFindFastDuplicatedResponse BuildPasienResponse(PasienModel pasien)
    {
        // Return respon sementara
        return new PasienFindFastDuplicatedResponse(
            pasien.PasienId,
            pasien.PasienName,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.TglMedrec.ToString(DateFormatEnum.YMD)
        );
    }
}

public class PasienFindFastDuplicatedTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly PasienFindDuplicatedHandler _sut;

    public PasienFindFastDuplicatedTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _sut = new PasienFindDuplicatedHandler(_pasienDal.Object); 
    }

    [Fact]
    public async Task GivenEjaanLamaPasienName_ThenReturnThePasienWithEjaanBaru()
    {
        var faker1 = new PasienModel("A", "Budi Subur Husnuloh");
        var faker2 = new PasienModel("B", "Boedi Soeboer Hoesnoeloh");

        // Mock the data retrieval methods
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> { faker1 });
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFastDuplicated("A");
        
        var response = await _sut.Handle(request, CancellationToken.None);
        response.First().PasienId.Should().Be("A");
    }

    [Fact]
    public async Task GivenEjaanBaruPasienName_ThenReturnThePasienWithEjaanLama()
    {
        var faker1 = new PasienModel("A", "Boedi Soeboer Hoesnoeloh");
        var faker2 = new PasienModel("B", "Budi Subur Husnuloh");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> { faker1 });
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFastDuplicated("A");
        
        var response = await _sut.Handle(request, CancellationToken.None);
        response.First().PasienId.Should().Be("A");
        
    }
}
