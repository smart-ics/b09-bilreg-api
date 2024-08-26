using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanGetQuery(string KecamatanId): IRequest<KecamatanGetResponse>, IKecamatanKey;

public record KecamatanGetResponse(
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName);
    
public class KecamatanGetHandler: IRequestHandler<KecamatanGetQuery, KecamatanGetResponse>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanGetHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }
    
    public Task<KecamatanGetResponse> Handle(KecamatanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kecamatanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kecamatan id: {request.KecamatanId} not found");

        // RESPONSE
        var response = new KecamatanGetResponse(result.KecamatanId, result.KecamatanName, result.KabupatenId,
            result.KabupatenName);
        return Task.FromResult(response);
    }
}

public class KecamatanGetHandlerTest
{
    private readonly Mock<IKecamatanDal> _kecamatanDal;
    private readonly KecamatanGetHandler _sut;
    
    public KecamatanGetHandlerTest()
    {
        _kecamatanDal = new Mock<IKecamatanDal>();
        _sut = new KecamatanGetHandler(_kecamatanDal.Object);
    }

    [Fact]
    public async Task GivenInvalidKecamatanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KecamatanGetQuery("A");
        _kecamatanDal.Setup(x=> x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(null as KecamatanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidKecamatanId_ThenReturnExpected_Test()
    {
        var request = new KecamatanGetQuery("A");
        var expected = KecamatanModel.Create("A", "B");
        expected.Set(KabupatenModel.Create("C", "D"));
        _kecamatanDal.Setup(x => x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}