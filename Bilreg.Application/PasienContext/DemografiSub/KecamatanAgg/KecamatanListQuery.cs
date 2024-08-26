using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanListQuery(string KabupatenId): IRequest<IEnumerable<KecamatanListResponse>>, IKabupatenKey;

public record KecamatanListResponse(
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName
    );
    
public class KecamatanListHandler: IRequestHandler<KecamatanListQuery, IEnumerable<KecamatanListResponse>>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanListHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }

    public Task<IEnumerable<KecamatanListResponse>> Handle(KecamatanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kecamatanDal.ListData(request)
            ?? throw new KeyNotFoundException("Kecamatan not found");
        
        // RESPONSE
        var response = result.Select(x
            => new KecamatanListResponse(x.KecamatanId, x.KecamatanName, x.KabupatenId, x.KabupatenName));
        return Task.FromResult(response);
    }
}

public class KecamatanListHandlerTest
{
    private readonly Mock<IKecamatanDal> _kecamatanDal;
    private readonly KecamatanListHandler _sut;

    public KecamatanListHandlerTest()
    {
        _kecamatanDal = new Mock<IKecamatanDal>();
        _sut = new KecamatanListHandler(_kecamatanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KecamatanListQuery("A");
        _kecamatanDal.Setup(x => x.ListData(It.IsAny<IKabupatenKey>()))
            .Returns(null as IEnumerable<KecamatanModel>);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new KecamatanListQuery("A");
        var expected = new List<KecamatanModel>() { KecamatanModel.Create("A", "B") };
        _kecamatanDal.Setup(x => x.ListData(It.IsAny<IKabupatenKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        var expectedResponse = expected.Select(x
            => new KecamatanListResponse(x.KecamatanId, x.KecamatanName, x.KabupatenId, x.KabupatenName));

        actual.Should().BeEquivalentTo(expectedResponse);
    }
}