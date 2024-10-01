using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifListQuery() : IRequest<IEnumerable<TipeTarifListResponse>>;

public record TipeTarifListResponse(
    string TipeTarifId,
    string TipeTarifName,
    bool IsAktif,
    decimal NoUrut);

public class TipeTarifListHandler : IRequestHandler<TipeTarifListQuery, IEnumerable<TipeTarifListResponse>>
{
    private readonly ITipeTarifDal _tipeTarifDal;

    public TipeTarifListHandler(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    
    public Task<IEnumerable<TipeTarifListResponse>> Handle(TipeTarifListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tipeTarifList = _tipeTarifDal.ListData()
                            ?? throw new KeyNotFoundException("Tipe Tarif not found");
        
        // RESPONSE
        var response = tipeTarifList.Select(x => new TipeTarifListResponse(
            x.TipeTarifId,
            x.TipeTarifName,
            x.IsAktif,
            x.NoUrut));
        
        return Task.FromResult(response);
    }
}

public class TipeTarifListHandlerTest
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDal;
    private readonly TipeTarifListHandler _sut;

    public TipeTarifListHandlerTest()
    {
        _tipeTarifDal = new Mock<ITipeTarifDal>();
        _sut = new TipeTarifListHandler(_tipeTarifDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new TipeTarifListQuery();
        _tipeTarifDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<TipeTarifModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
}