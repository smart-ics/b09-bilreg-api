using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifGetQuery(string TipeTarifId) : IRequest<TipeTarifGetResponse>, ITipeTarifKey;

public record TipeTarifGetResponse(
    string TipeTarifId,
    string TipeTarifName,
    bool IsAktif,
    decimal NoUrut);
    
public class TipeTarifGetHandler : IRequestHandler<TipeTarifGetQuery, TipeTarifGetResponse>
{
    private readonly ITipeTarifDal _tipeTarifDal;

    public TipeTarifGetHandler(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    
    public Task<TipeTarifGetResponse> Handle(TipeTarifGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tipeTarif = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"No tipe tarif with id {request.TipeTarifId}");
        
        // RESPONSE
        var response = new TipeTarifGetResponse(
            tipeTarif.TipeTarifId,
            tipeTarif.TipeTarifName,
            tipeTarif.IsAktif,
            tipeTarif.NoUrut
            );
        
        return Task.FromResult(response);
    }
}

public class TipeTarifGetHandlerTest
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDal;
    private readonly TipeTarifGetHandler _sut;

    public TipeTarifGetHandlerTest()
    {
        _tipeTarifDal = new Mock<ITipeTarifDal>();
        _sut = new TipeTarifGetHandler(_tipeTarifDal.Object);
    }

    [Fact]
    public async Task GivenInvalidTipeTarifId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new TipeTarifGetQuery("A");
        _tipeTarifDal.Setup(x => x.GetData(It.IsAny<TipeTarifGetQuery>()))
            .Returns(null as TipeTarifModel);
        var actual = async () => await _sut.Handle(request, new CancellationToken());
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}