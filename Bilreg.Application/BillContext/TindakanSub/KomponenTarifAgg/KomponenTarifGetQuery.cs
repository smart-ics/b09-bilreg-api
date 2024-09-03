using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public record KomponenTarifGetQuery(string KomponenId): IRequest<KomponenTarifGetResponse>, IKomponenTarifKey;

public record KomponenTarifGetResponse(string KomponenId, string KomponenName);

public class KomponenTarifGetHandler: IRequestHandler<KomponenTarifGetQuery, KomponenTarifGetResponse>
{
    private readonly IKomponenTarifDal _komponenTarifDal;

    public KomponenTarifGetHandler(IKomponenTarifDal komponenTarifDal)
    {
        _komponenTarifDal = komponenTarifDal;
    }

    public Task<KomponenTarifGetResponse> Handle(KomponenTarifGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var komponenTarif = _komponenTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Komponen tarif with id {request.KomponenId} not found");

        // RESPONSE
        var response = new KomponenTarifGetResponse(komponenTarif.KomponenId, komponenTarif.KomponenName);
        return Task.FromResult(response);
    }
}

public class KomponenTarifGetHandlerTest
{
    private readonly Mock<IKomponenTarifDal> _komponenTarifDal;
    private readonly KomponenTarifGetHandler _sut;

    public KomponenTarifGetHandlerTest()
    {
        _komponenTarifDal = new Mock<IKomponenTarifDal>();
        _sut = new KomponenTarifGetHandler(_komponenTarifDal.Object);
    }
    
    [Fact]
    public async Task GivenInvalidKomponenId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KomponenTarifGetQuery("A");
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenTarifKey>()))
            .Returns(null as KomponenTarifModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidKomponenId_ThenReturnExpected_Test()
    {
        var request = new KomponenTarifGetQuery("A");
        var expected = new KomponenTarifModel("A", "B");
        var expectedResponse =
            new KomponenTarifGetResponse(expected.KomponenId, expected.KomponenName);
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenTarifKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}