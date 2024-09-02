using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanListQuery() : IRequest<IEnumerable<JaminanListResponse>>;

public record JaminanListResponse(
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

public class JaminanListHandler : IRequestHandler<JaminanListQuery, IEnumerable<JaminanListResponse>>
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanListHandler(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public Task<IEnumerable<JaminanListResponse>> Handle(JaminanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var listJaminan = _jaminanDal.ListData()
            ?? throw new KeyNotFoundException("Jaminan not found");

        // RESPONSE
        var response = listJaminan.Select(x 
            => new JaminanListResponse(x.JaminanId, x.JaminanName, x.Alamat1, x.Alamat2,
                x.Kota, x.IsAktif, x.CaraBayarDkId, x.CaraBayarDkName, x.GrupJaminanId,
                x.GrupJaminanName, x.BenefitMou));
        return Task.FromResult(response);
    }
}

public class JaminanListHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly JaminanListHandler _sut;

    public JaminanListHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _sut = new JaminanListHandler(_jaminanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanListQuery();
        _jaminanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<JaminanModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}