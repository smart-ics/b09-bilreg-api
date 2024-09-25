using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalGetQuery(string BangsalId) : IRequest<BangsalGetResponse>, IBangsalKey;

public record BangsalGetResponse(
    string BangsalId,
    string BangsalName,
    string LayananId,
    string LayananName);

public class BangsalGetHandler : IRequestHandler<BangsalGetQuery, BangsalGetResponse>
{
    private readonly IBangsalDal _bangsalDal;

    public BangsalGetHandler(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    
    public Task<BangsalGetResponse> Handle(BangsalGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var bangsal = _bangsalDal.GetData(request)
            ?? throw new KeyNotFoundException($"Bangsal id {request.BangsalId} not found");

        // RESPONSE
        var response = new BangsalGetResponse(
            bangsal.BangsalId,
            bangsal.BangsalName,
            bangsal.LayananId,
            bangsal.LayananName
            );
        
        return Task.FromResult(response);

    }
}

public class BangsalGetHandlerTest
{
    private readonly Mock<IBangsalDal> _bangsalDal;
    private readonly BangsalGetHandler _sut;

    public BangsalGetHandlerTest()
    {
        _bangsalDal = new Mock<IBangsalDal>();
        _sut = new BangsalGetHandler(_bangsalDal.Object);
    }

    [Fact]
    public async Task GivenInvalidBangsalId_ThenThrowKeyNotFoundException()
    {
        var request = new BangsalGetQuery("A");
        _bangsalDal.Setup(x => x.GetData(It.IsAny<IBangsalKey>()))
            .Returns(null as BangsalModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
        
    }
    
    [Fact]
    public async Task GivenvalidBangsalId_ThenThrowKeyNotFoundException()
    {
        var request = new BangsalGetQuery("A");
        var expected = new BangsalModel("A","B");
        _bangsalDal.Setup(x => x.GetData(It.IsAny<IBangsalKey>()))
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
        
    }
}