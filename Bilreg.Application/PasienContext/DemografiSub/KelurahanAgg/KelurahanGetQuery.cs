using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanGetQuery(string KelurahanId): IRequest<KelurahanGetResponse>, IKelurahanKey;

public record KelurahanGetResponse(
    string KelurahanId,
    string KelurahanName,
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName,
    string KodePos
    );

public class KelurahanGetHandler: IRequestHandler<KelurahanGetQuery, KelurahanGetResponse>
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanGetHandler(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }
    
    public Task<KelurahanGetResponse> Handle(KelurahanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelurahanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelurahan id {request.KelurahanId} not found");
        
        // RESPONSE
        var response = new KelurahanGetResponse(
            result.KelurahanId, result.KelurahanName, result.KecamatanId, result.KecamatanName,
            result.KabupatenId, result.KabupatenName, result.PropinsiId,
            result.PropinsiName, result.KodePos);
        return Task.FromResult(response);
    }
}

public class KelurahanGetHandlerTest
{
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly KelurahanGetHandler _sut;

    public KelurahanGetHandlerTest()
    {
        _kelurahanDal = new Mock<IKelurahanDal>();
        _sut = new KelurahanGetHandler(_kelurahanDal.Object);
    }

    [Fact]
    public async Task GivenInvalidKelurahanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KelurahanGetQuery("A");
        _kelurahanDal.Setup(x => x.GetData(It.IsAny<IKelurahanKey>()))
            .Returns(null as KelurahanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidKelurahanId_ThenReturnData_Test()
    {
        var request = new KelurahanGetQuery("A");
        var expected = KelurahanModel.Create("A", "B", "C");
        var kecamatan = KecamatanModel.Create("D", "E");
        var kabupaten = KabupatenModel.Create("F", "G");
        kabupaten.Set(PropinsiModel.Create("H", "I"));
        kecamatan.Set(kabupaten);
        expected.Set(kecamatan);
        
        _kelurahanDal.Setup(x => x.GetData(It.IsAny<IKelurahanKey>()))
            .Returns(expected);
        
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}