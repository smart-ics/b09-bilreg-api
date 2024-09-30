using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedListDataByBangsalId(string BangsalId) : IRequest<IEnumerable<BedListDataByBangsalIdResponse>>, IBangsalKey;

public record BedListDataByBangsalIdResponse(
    string BedId,
    string BedName,
    string Keterangan,
    bool IsAktif,
    string KamarId,
    string KamarName,
    string BangsalId,
    string BangsalName);

public class BedListDataByBangsalIdHandler : IRequestHandler<BedListDataByBangsalId, IEnumerable<BedListDataByBangsalIdResponse>>
{
    private readonly IBedDal _bedDal;

    public BedListDataByBangsalIdHandler(IBedDal bedDal)
    {
        _bedDal = bedDal;
    }
    public Task<IEnumerable<BedListDataByBangsalIdResponse>> Handle(BedListDataByBangsalId request, CancellationToken cancellationToken)
    {
        // QUERY
        var bed = _bedDal.ListData(request)
            ?? throw new KeyNotFoundException($"Cannot find Bed with BangsalId");
        
        // RESPONSE
        var response = bed.Select(x => new BedListDataByBangsalIdResponse(
            x.BedId,
            x.BedName,
            x.Keterangan,
            x.IsAktif,
            x.KamarId,
            x.KamarName,
            x.BangsalId,
            x.BangsalName));
        
        return Task.FromResult(response);
    }
}

public class BedListDataByBangsalIdHandlerTest
{
    private readonly Mock<IBedDal> _bedDal;
    private readonly BedListDataByBangsalIdHandler _sut;

    public BedListDataByBangsalIdHandlerTest()
    {
        _bedDal = new Mock<IBedDal>();
        _sut = new BedListDataByBangsalIdHandler(_bedDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new BedListDataByBangsalId("");
        _bedDal.Setup(x => x.ListData(request))
            .Returns(null as IEnumerable<BedModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}