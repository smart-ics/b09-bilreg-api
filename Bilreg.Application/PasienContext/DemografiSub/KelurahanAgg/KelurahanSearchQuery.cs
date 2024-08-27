using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanSearchQuery(string Keyword): IRequest<IEnumerable<KelurahanSearchResponse>>;

public record KelurahanSearchResponse(
    string KelurahanId,
    string KelurahanName,
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName,
    string KodePos
);

public class KelurahanSearchHandler: IRequestHandler<KelurahanSearchQuery, IEnumerable<KelurahanSearchResponse>>
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanSearchHandler(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }

    public Task<IEnumerable<KelurahanSearchResponse>> Handle(KelurahanSearchQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelurahanDal.ListData(request.Keyword)
            ?? throw new KeyNotFoundException($"Kelurahan {request.Keyword} not found");
        
        // RESPONSE
        var response = result.Select(x => new KelurahanSearchResponse(x.KelurahanId, x.KelurahanName, x.KecamatanId,
            x.KecamatanName, x.KabupatenId, x.KabupatenName, x.PropinsiId, x.PropinsiName, x.KodePos));
        return Task.FromResult(response);
    }
}

public class KelurahanSearchHandlerTest
{
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly KelurahanSearchHandler _sut;

    public KelurahanSearchHandlerTest()
    {
        _kelurahanDal = new Mock<IKelurahanDal>();
        _sut = new KelurahanSearchHandler(_kelurahanDal.Object);
    }
    
    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KelurahanSearchQuery("A");
        _kelurahanDal.Setup(x => x.ListData(It.IsAny<string>()))
            .Returns(null as IEnumerable<KelurahanModel>);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new KelurahanSearchQuery("A");
        var expected = new List<KelurahanModel>() { KelurahanModel.Create("A", "B", "C") };
        _kelurahanDal.Setup(x => x.ListData(It.IsAny<string>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        var expectedResponse = expected.Select(x => new KelurahanListResponse(x.KelurahanId, x.KelurahanName,
            x.KecamatanId, x.KecamatanName, x.KabupatenId, x.KabupatenName, x.PropinsiId, x.PropinsiName, x.KodePos));
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}