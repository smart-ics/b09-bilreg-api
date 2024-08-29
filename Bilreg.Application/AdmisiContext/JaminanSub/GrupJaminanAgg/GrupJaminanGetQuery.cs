using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanGetQuery(string GrupJaminanId): IRequest<GrupJaminanGetResponse>, IGrupJaminanKey;

public record GrupJaminanGetResponse(
    string GrupJaminanId,
    string GrupJaminanName,
    bool IsKaryawan,
    string Keterangan);
    
public class GrupJaminanGetHandler: IRequestHandler<GrupJaminanGetQuery, GrupJaminanGetResponse>
{
    private readonly IGrupJaminanDal _grupJaminanDal;

    public GrupJaminanGetHandler(IGrupJaminanDal grupJaminanDal)
    {
        _grupJaminanDal = grupJaminanDal;
    }

    public Task<GrupJaminanGetResponse> Handle(GrupJaminanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _grupJaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Grup jaminan id {request.GrupJaminanId} not found");
        
        // RESPONSE
        var response = new GrupJaminanGetResponse(result.GrupJaminanId, result.GrupJaminanName, result.IsKaryawan,
            result.Keterangan);
        return Task.FromResult(response);
    }
}

public class GrupJaminanGetHandlerTest
{
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly GrupJaminanGetHandler _sut;

    public GrupJaminanGetHandlerTest()
    {
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _sut = new GrupJaminanGetHandler(_grupJaminanDal.Object);
    }

    [Fact]
    public async Task GivenInvalidGrupJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new GrupJaminanGetQuery("A");
        _grupJaminanDal.Setup(x => x.GetData(It.IsAny<IGrupJaminanKey>()))
            .Returns(null as GrupJaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidGrupJaminanId_ThenReturnExpected_Test()
    {
        var request = new GrupJaminanGetQuery("A");
        var expected = GrupJaminanModel.Create("A", "B", "C");
        expected.Set(false);
        var expectedResponse =
            new GrupJaminanGetResponse(expected.GrupJaminanId, expected.GrupJaminanName, expected.IsKaryawan, expected.Keterangan);
        _grupJaminanDal.Setup(x => x.GetData(It.IsAny<IGrupJaminanKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}