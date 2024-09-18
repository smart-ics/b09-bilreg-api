using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public record GrupTarifListQuery(): IRequest<IEnumerable<GrupTarifListResponse>>;

public record GrupTarifListResponse(string GrupTarifId, string GrupTarifName);

public class GrupTarifListHandler: IRequestHandler<GrupTarifListQuery, IEnumerable<GrupTarifListResponse>>
{
    private readonly IGrupTarifDal _grupTarifDal;

    public GrupTarifListHandler(IGrupTarifDal grupTarifDal)
    {
        _grupTarifDal = grupTarifDal;
    }

    public Task<IEnumerable<GrupTarifListResponse>> Handle(GrupTarifListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var grupTarifList = _grupTarifDal.ListData()
            ?? throw new KeyNotFoundException("Grup tarif not found");

        // RESPONSE
        var response = grupTarifList.Select(x => new GrupTarifListResponse(x.GrupTarifId, x.GrupTarifName));
        return Task.FromResult(response);
    }
}

public class GrupTarifListHandlerTest
{
    private readonly Mock<IGrupTarifDal> _grupTarifDal;
    private readonly GrupTarifListHandler _sut;

    public GrupTarifListHandlerTest()
    {
        _grupTarifDal = new Mock<IGrupTarifDal>();
        _sut = new GrupTarifListHandler(_grupTarifDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new GrupTarifListQuery();
        _grupTarifDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<GrupTarifModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidData_ThenReturnExpectedResponse()
    {
        var request = new GrupTarifListQuery();
        var expected = new GrupTarifModel("A", "B");
        _grupTarifDal.Setup(x => x.ListData())
            .Returns(new List<GrupTarifModel>() { expected });
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}