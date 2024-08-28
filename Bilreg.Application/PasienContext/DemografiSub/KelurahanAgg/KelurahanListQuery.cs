using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanListQuery(string KecamatanId): IRequest<IEnumerable<KelurahanListResponse>>, IKecamatanKey;

public record KelurahanListResponse(
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

public class KelurahanListHandler: IRequestHandler<KelurahanListQuery, IEnumerable<KelurahanListResponse>>
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanListHandler(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }
    
    public Task<IEnumerable<KelurahanListResponse>> Handle(KelurahanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelurahanDal.ListData(request)
            ?? throw new KeyNotFoundException("Kelurahan not found");
        
        // RESPONSE
        var response = result.Select(x => new KelurahanListResponse(x.KelurahanId, x.KelurahanName, x.KecamatanId,
            x.KecamatanName, x.KabupatenId, x.KabupatenName, x.PropinsiId, x.PropinsiName, x.KodePos));
        return Task.FromResult(response);
    }
}

public class KelurahanListHandlerTest
{
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly KelurahanListHandler _sut;

    public KelurahanListHandlerTest()
    {
        _kelurahanDal = new Mock<IKelurahanDal>();
        _sut = new KelurahanListHandler(_kelurahanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KelurahanListQuery("A");
        _kelurahanDal.Setup(x => x.ListData(It.IsAny<IKecamatanKey>()))
            .Returns(null as IEnumerable<KelurahanModel>);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new KelurahanListQuery("A");
        var expected = new List<KelurahanModel>() { KelurahanModel.Create("A", "B", "C") };
        _kelurahanDal.Setup(x => x.ListData(It.IsAny<IKecamatanKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        var expectedResponse = expected.Select(x => new KelurahanListResponse(x.KelurahanId, x.KelurahanName,
            x.KecamatanId, x.KecamatanName, x.KabupatenId, x.KabupatenName, x.PropinsiId, x.PropinsiName, x.KodePos));
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}