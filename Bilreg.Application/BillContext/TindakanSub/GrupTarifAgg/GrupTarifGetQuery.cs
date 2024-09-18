using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public record GrupTarifGetQuery(string GrupTarifId): IRequest<GrupTarifGetResponse>, IGrupTarifKey;

public record GrupTarifGetResponse(string GrupTarifId, string GrupTarifName);

public class GrupTarifGetHandler: IRequestHandler<GrupTarifGetQuery, GrupTarifGetResponse>
{
    private readonly IGrupTarifDal _grupTarifDal;

    public GrupTarifGetHandler(IGrupTarifDal grupTarifDal)
    {
        _grupTarifDal = grupTarifDal;
    }

    public Task<GrupTarifGetResponse> Handle(GrupTarifGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var grupTarif = _grupTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Grup tarif with id: {request.GrupTarifId} not found");
        
        // RESPONSE
        var response = new GrupTarifGetResponse(grupTarif.GrupTarifId, grupTarif.GrupTarifName);
        return Task.FromResult(response);
    }
}

public class GrupTarifGetHandlerTest
{
    private readonly Mock<IGrupTarifDal> _grupTarifDal;
    private readonly GrupTarifGetHandler _sut;

    public GrupTarifGetHandlerTest()
    {
        _grupTarifDal = new Mock<IGrupTarifDal>();
        _sut = new GrupTarifGetHandler(_grupTarifDal.Object);
    }

    [Fact]
    public async Task GivenInvalidGrupTarifId_ThenThrowKeyNotFoundException()
    {
        var request = new GrupTarifGetQuery("A");
        _grupTarifDal.Setup(x => x.GetData(It.IsAny<IGrupTarifKey>()))
            .Returns(null as GrupTarifModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidGrupTarifId_ThenReturnExpected()
    {
        var request = new GrupTarifGetQuery("A");
        var expected = new GrupTarifModel("A", "B");
        _grupTarifDal.Setup(x => x.GetData(It.IsAny<IGrupTarifKey>()))
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}