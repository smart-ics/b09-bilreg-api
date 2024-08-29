using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanListQuery(): IRequest<IEnumerable<GrupJaminanListResponse>>;

public record GrupJaminanListResponse(
    string GrupJaminanId,
    string GrupJaminanName,
    bool IsKaryawan,
    string Keterangan
    );

public class GrupJaminanListHandler: IRequestHandler<GrupJaminanListQuery, IEnumerable<GrupJaminanListResponse>>
{
    private readonly IGrupJaminanDal _grupJaminanDal;

    public GrupJaminanListHandler(IGrupJaminanDal grupJaminanDal)
    {
        _grupJaminanDal = grupJaminanDal;
    }

    public Task<IEnumerable<GrupJaminanListResponse>> Handle(GrupJaminanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _grupJaminanDal.ListData()
            ?? throw new KeyNotFoundException("Grup jaminan not found");
        
        // RESPONSE
        var response = result.Select(x =>
            new GrupJaminanListResponse(x.GrupJaminanId, x.GrupJaminanName, x.IsKaryawan, x.Keterangan));
        return Task.FromResult(response);
    }
}

public class GrupJaminanListHandlerTest
{
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly GrupJaminanListHandler _sut;

    public GrupJaminanListHandlerTest()
    {
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _sut = new GrupJaminanListHandler(_grupJaminanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new GrupJaminanListQuery();
        _grupJaminanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<GrupJaminanModel>);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new GrupJaminanListQuery();
        var expected = new List<GrupJaminanModel>() { GrupJaminanModel.Create("A", "B", "C") };
        _grupJaminanDal.Setup(x => x.ListData())
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}