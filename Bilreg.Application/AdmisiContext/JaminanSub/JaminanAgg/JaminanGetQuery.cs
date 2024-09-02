using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanGetQuery(string JaminanId) : IRequest<JaminanGetResponse>, IJaminanKey;

public record JaminanGetResponse(
    string JaminanId,
    string JaminanName,
    string Alamat1,
    string Alamat2,
    string Kota,
    bool IsAktif,
    string CaraBayarDkId,
    string CaraBayarDkName,
    string GrupJaminanId,
    string GrupJaminanName,
    string BenefitMou
);

public class JaminanGetHandler : IRequestHandler<JaminanGetQuery, JaminanGetResponse>
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanGetHandler(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public Task<JaminanGetResponse> Handle(JaminanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var jaminan = _jaminanDal.GetData(request) 
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");

        // RESPONSE
        var response = new JaminanGetResponse(jaminan.JaminanId, jaminan.JaminanName, jaminan.Alamat1, jaminan.Alamat2,
            jaminan.Kota, jaminan.IsAktif, jaminan.CaraBayarDkId, jaminan.CaraBayarDkName, jaminan.GrupJaminanId,
            jaminan.GrupJaminanName, jaminan.BenefitMou);
        return Task.FromResult(response);
    }
}

public class JaminanGetHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly JaminanGetHandler _sut;

    public JaminanGetHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _sut = new JaminanGetHandler(_jaminanDal.Object);
    }

    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanGetQuery("A");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}