using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public record KomponenTarifListQuery(): IRequest<IEnumerable<KomponenTarifListResponse>>;

public record KomponenTarifListResponse(string KomponenId, string KomponenName);

public class KomponenTarifListHandler: IRequestHandler<KomponenTarifListQuery, IEnumerable<KomponenTarifListResponse>>
{
    private readonly IKomponenTarifDal _komponenTarifDal;

    public KomponenTarifListHandler(IKomponenTarifDal komponenTarifDal)
    {
        _komponenTarifDal = komponenTarifDal;
    }

    public Task<IEnumerable<KomponenTarifListResponse>> Handle(KomponenTarifListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var komponenTarifList = _komponenTarifDal.ListData()
            ?? throw new KeyNotFoundException("Komponen tarif not found");

        // RESPONSE
        var response = komponenTarifList.Select(x 
            => new KomponenTarifListResponse(x.KomponenId, x.KomponenName));
        return Task.FromResult(response);
    }
}

public class KomponenTarifListHandlerTest
{
    private readonly Mock<IKomponenTarifDal> _komponenTarifDal;
    private readonly KomponenTarifListHandler _sut;

    public KomponenTarifListHandlerTest()
    {
        _komponenTarifDal = new Mock<IKomponenTarifDal>();
        _sut = new KomponenTarifListHandler(_komponenTarifDal.Object);
    }
    
    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KomponenTarifListQuery();
        _komponenTarifDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<KomponenTarifModel>);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new KomponenTarifListQuery();
        var expected = new KomponenTarifModel("A", "B");
        var komponenTarifList = new List<KomponenTarifModel>() { expected };
        _komponenTarifDal.Setup(x => x.ListData())
            .Returns(komponenTarifList);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}