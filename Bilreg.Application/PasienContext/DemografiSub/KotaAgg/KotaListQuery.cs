using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public record KotaListQuery(): IRequest<IEnumerable<KotaListResponse>>;

public record KotaListResponse(string KotaId, string KotaName);

public class KotaListHandler: IRequestHandler<KotaListQuery, IEnumerable<KotaListResponse>>
{
    private readonly IKotaDal _kotaDal;

    public KotaListHandler(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public Task<IEnumerable<KotaListResponse>> Handle(KotaListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kotaDal.ListData()
            ?? throw new KeyNotFoundException("Kota not found");

        // RESPONSE
        var response = result.Select(x => new KotaListResponse(x.KotaId, x.KotaName));
        return Task.FromResult(response);
    }
}

public class KotaListHandlerTest
{
    private readonly Mock<IKotaDal> _kotaDal;
    private readonly KotaListHandler _sut;

    public KotaListHandlerTest()
    {
        _kotaDal = new Mock<IKotaDal>();
        _sut = new KotaListHandler(_kotaDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KotaListQuery();
        _kotaDal.Setup(x => x.ListData()).Returns(null as IEnumerable<KotaModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new KotaListQuery();
        var expected = new List<KotaModel>() { KotaModel.Create("A", "B") };
        var expectedResponse = expected.Select(x => new KotaListResponse(x.KotaId, x.KotaName));
        _kotaDal.Setup(x => x.ListData()).Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}