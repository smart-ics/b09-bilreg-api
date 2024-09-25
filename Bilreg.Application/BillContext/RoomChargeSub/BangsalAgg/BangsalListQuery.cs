﻿using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalListQuery() : IRequest<IEnumerable<BangsalListResponse>>;

public record BangsalListResponse(
    string BangsalId,
    string BangsalName,
    string LayananId,
    string LayananName);
    
public class BangsalListHandler : IRequestHandler<BangsalListQuery, IEnumerable<BangsalListResponse>>
{
    private readonly IBangsalDal _bangsalDal;

    public BangsalListHandler(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    
    public Task<IEnumerable<BangsalListResponse>> Handle(BangsalListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var bangsalList = _bangsalDal.ListData()
            ?? throw new KeyNotFoundException("BangsalList not found");
        
        // RESPONSE
        var response = bangsalList.Select(x => new BangsalListResponse(
            x.BangsalId,
            x.BangsalName,
            x.LayananId,
            x.LayananName));
        return Task.FromResult(response);
    }
}

public class BangsalListHanlerTest
{
    private readonly Mock<IBangsalDal> _bangsalDal;
    private BangsalListHandler _sut;

    public BangsalListHanlerTest()
    {
        _bangsalDal = new Mock<IBangsalDal>();
        _sut = new BangsalListHandler(_bangsalDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new BangsalListQuery();
        _bangsalDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<BangsalModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidData_ThenReturnExpectedResponse()
    {
        var request = new BangsalListQuery();
        var expected = new BangsalModel("A", "B");
        _bangsalDal.Setup(x => x.ListData())
            .Returns(new List<BangsalModel> { expected });
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}