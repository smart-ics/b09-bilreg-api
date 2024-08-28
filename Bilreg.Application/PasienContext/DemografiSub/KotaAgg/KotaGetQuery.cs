using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public record KotaGetQuery(string KotaId): IRequest<KotaGetResponse>, IKotaKey;

public record KotaGetResponse(string KotaId, string KotaName);

public class KotaGetHandler: IRequestHandler<KotaGetQuery, KotaGetResponse>
{
    private readonly IKotaDal _kotaDal;

    public KotaGetHandler(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }
    
    public Task<KotaGetResponse> Handle(KotaGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kotaDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kota id {request.KotaId} not found");

        // RESPONSE
        var response = new KotaGetResponse(result.KotaId, result.KotaName);
        return Task.FromResult(response);
    }
}

public class KotaGetHandlerTest
{
    private readonly Mock<IKotaDal> _kotaDal;
    private readonly KotaGetHandler _sut;

    public KotaGetHandlerTest()
    {
        _kotaDal = new Mock<IKotaDal>();
        _sut = new KotaGetHandler(_kotaDal.Object);
    }

    [Fact]
    public async Task GivenInvalidKotaId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KotaGetQuery("A");
        _kotaDal.Setup(x => x.GetData(It.IsAny<IKotaKey>()))
            .Returns(null as KotaModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidKotaId_ThenReturnExpected_Test()
    {
        var request = new KotaGetQuery("A");
        var expected = KotaModel.Create("A", "B");
        var expectedResponse = new KotaGetResponse(expected.KotaId, expected.KotaName);
        _kotaDal.Setup(x => x.GetData(It.IsAny<IKotaKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}