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
        
        var variasiEjaan = GenerateVariasiEjaan(pasien.PasienName);
        var distinctWords = variasiEjaan
            .SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resultEjaanByWords = FindByWords(listPasien, distinctWords, pasien.PasienName);
        
        var result = resultJw.Union(resultEjaanByWords);
        
        var response = result.Select(BuildPasienResponse);
        return await Task.FromResult(response);
    }

    private IEnumerable<PasienModel> FindByWords(IEnumerable<PasienModel> listPasien, 
        IEnumerable<string> wordsVariasiEjaan, string searchKeyword)
    {
        var variasiWordCount = searchKeyword.Split(' ').Length;
        var wordCountMin = Math.Min(variasiWordCount, 2);
        var result = new List<PasienModel>();
        foreach (var pasien in listPasien)
        {
            var pasienNameClean = RemovePunctuation(pasien.PasienName);
            var listWords = pasienNameClean.Split(' ');
            var found = listWords
                .Count(wordPasien => wordsVariasiEjaan
                    .Any(item => item
                        .Equals(wordPasien, StringComparison.CurrentCultureIgnoreCase)));
            if (found >= wordCountMin)
                result.Add(pasien);
        }
        return result;
    }

    private static string RemovePunctuation(string input)
    {
        return new string(input.Where(c => !char.IsPunctuation(c)).ToArray());
    }

    private record Ejaan(string Eja1, string Eja2);

    private static List<string> GenerateVariasiEjaan(string originName)
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
        
        //  membuat collection variasi ejaan original name
        //  contoh: Budi Agung Sutejo => Boedi Agoeng Soetedjo
        var result = spellingVariations
            .Aggregate(new List<string> { cleanOriginName }, (variants, entry) => variants
                .Concat(variants
                    .Where(name => name.Contains(entry.Eja1, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name.Replace(entry.Eja1, entry.Eja2, StringComparison.OrdinalIgnoreCase))
                ).Distinct().ToList()
            );
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
    public async Task T01_GivenSimiliarPasienName_ThenReturnThePasien()
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
    public async Task T02_GivenNotSimiliarPasienName_ThenReturnThePasien()
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
    public async Task T03_GivenEjaanLamaPasienName_ThenReturnThePasienWithEjaanBaru()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Subur Husnuloh");
        var faker2 = new PasienModel("B", "Boedi Soeboer Hoesnoeloh");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }
    
    [Fact]
    public async Task T04_GivenEjaanBaruPasienName_ThenReturnThePasienWithEjaanLama()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Boedi Soeboer Hoesnoeloh");
        var faker2 = new PasienModel("B", "Budi Subur Husnuloh");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.First().PasienId.Should().Be("A");
    }
    
    [Fact]
    public async Task T05_GivenNamaSamaSebagianEjaanLama_ThenReturnThePasienWithEjaanBaru()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Subur Husnuloh");
        var faker2 = new PasienModel("B", "Boedi Soeboer");
        var faker1A = new PasienModel("C", "Sunardinata");
        var faker1B = new PasienModel("D", "Budi Subur");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1, faker1A, faker1B});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A","D");
    }    

    [Fact]
    public async Task T06_GivenNamaSamaSebagianEjaanBaru_ThenReturnThePasienWithEjaanLama()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Boedi Soeboer Hoesnoeloh");
        var faker2 = new PasienModel("B", "Budi Subur");
        var faker1A = new PasienModel("C", "Sunardinata");
        var faker1B = new PasienModel("D", "Budi Subur");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1, faker1A, faker1B});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A","D");
    }    

    
    [Fact]
    public async Task T07_GivenUrutanNamaBerbeda_ThenReturnThePasien()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Agung Deindra Susanto");
        var faker2 = new PasienModel("B", "Soesanto Agung Deindra");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }    

    [Fact]
    public async Task T08_GivenFullName_ThenReturnFirstAndLastName()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Amanda Farel");
        var faker2 = new PasienModel("B", "Amanda Jessica Farel");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }    
    [Fact]
    public async Task T09_GivenFirstAndLastName_ThenReturnFullName()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Amanda Jessica Farel");
        var faker2 = new PasienModel("B", "Amanda Farel");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }   
    
    [Fact]
    public async Task T10_GivenNawaWithGelar_ThenReturnNamaTanpaGelar()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Susanto ");
        var faker2 = new PasienModel("B", "Budi Susanto, S.Kom");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }    
    
    [Fact]
    public async Task T11_GivenNamaTanpaGelar_ThenReturnNamaWithGelar()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Susanto, S.Kom");
        var faker2 = new PasienModel("B", "Susanto Budhi");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }   
    
    [Fact]
    public async Task T12_GivenNamaSatuKata_ThenReturnFullName()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Susanto, S.Kom");
        var faker2 = new PasienModel("B", "Susanto");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }       
    
    [Fact]
    public async Task T13_GivenNamaTengahDisingkat_ThenReturnFullName()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Susanto Kurniawan");
        var faker2 = new PasienModel("B", "Budi S. Kurniawan ");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }       

    [Fact]
    public async Task T14_GivenFullName_ThenReturnNamaTengahDisingkat()
    {
        //  ARRANGE
        var faker1 = new PasienModel("A", "Budi Susanto Kurniawan");
        var faker2 = new PasienModel("B", "Budi S. Kurniawan ");
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns(new List<PasienModel> {faker1});
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(faker2);
        var request = new PasienFindFast("B");
        
        //  ACT
        var response = await _sut.Handle(request, CancellationToken.None);
        
        //  ASSERT
        response.Select(x => x.PasienId).Should().BeEquivalentTo("A");
    }       
}

