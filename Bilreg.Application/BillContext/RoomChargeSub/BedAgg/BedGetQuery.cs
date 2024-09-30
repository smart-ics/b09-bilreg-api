using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedGetQuery(string BedId) : IRequest<BedGetResponse>, IBedKey;

public record BedGetResponse(
    string BedId,
    string BedName,
    string Keterangan,
    bool IsAktif,
    string KamarId,
    string KamarName,
    string BangsalId,
    string BangsalName);

public class BedGetHandler : IRequestHandler<BedGetQuery, BedGetResponse>
{
    private readonly IBedDal _bedDal;

    public BedGetHandler(IBedDal bedDal)
    {
        _bedDal = bedDal;
    }
    public Task<BedGetResponse> Handle(BedGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var bed = _bedDal.GetData(request)
            ?? throw new KeyNotFoundException($"BedId {request.BedId} not found");
        
        // RESPONSE
        var response = new BedGetResponse(
            bed.BedId,
            bed.BedName,
            bed.Keterangan,
            bed.IsAktif,
            bed.KamarId,
            bed.KamarName,
            bed.BangsalId,
            bed.BangsalName);
        
        return Task.FromResult(response);
    }
}

public class BedGetHandlerTest
{
    private readonly Mock<IBedDal> _bedDal;
    private readonly BedGetHandler _sut;

    public BedGetHandlerTest()
    {
        _bedDal = new Mock<IBedDal>();
        _sut = new BedGetHandler(_bedDal.Object);
    }

    [Fact]
    public async Task GivenInvalidBedId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new BedGetQuery("A");
        _bedDal.Setup(x => x.GetData(It.IsAny<BedGetQuery>()))
            .Returns(null as BedModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
}