using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceListQuery() : IRequest<IEnumerable<AmbulanceListResponse>>;

public record AmbulanceKomponenListResponse(
    string AmbulanceId,
    string KomponenId,
    decimal NilaiTarif,
    bool IsTetap
);

public record AmbulanceListResponse(
    string AmbulanceId,
    string AmbulanceName,
    bool IsAktif,
    decimal Abonement,
    IEnumerable<AmbulanceKomponenListResponse> ListKomponen
);

public class AmbulanceListHandler : IRequestHandler<AmbulanceListQuery, IEnumerable<AmbulanceListResponse>>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceDal _ambulanceDal;

    public AmbulanceListHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceDal ambulanceDal)
    {
        _factory = factory;
        _ambulanceDal = ambulanceDal;
    }

    public Task<IEnumerable<AmbulanceListResponse>> Handle(AmbulanceListQuery request,
        CancellationToken cancellationToken)
    {
        // QUERY
        var listAmbulance = _ambulanceDal.ListData()
            ?? throw new KeyNotFoundException("Ambulance not found");

        // RESPONSE
        var response = listAmbulance.Select(BuildAmbulanceListItem);
        return Task.FromResult(response);
    }

    private AmbulanceListResponse BuildAmbulanceListItem(IAmbulanceKey key)
    {
        var ambulance = _factory.Load(key);
        var listKomponen = ambulance.ListKomponen.Select(x =>
            new AmbulanceKomponenListResponse(x.AmbulanceId, x.KomponenId, x.NilaiTarif, x.IsTetap));
        var response = new AmbulanceListResponse(ambulance.AmbulanceId, ambulance.AmbulanceName, ambulance.IsAktif,
            ambulance.Abonement, listKomponen);
        return response;
    }
}

public class AmbulanceListHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IAmbulanceDal> _ambulanceDal;
    private readonly AmbulanceListHandler _sut;

    public AmbulanceListHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _ambulanceDal = new Mock<IAmbulanceDal>();
        _sut = new AmbulanceListHandler(_factory.Object, _ambulanceDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceListQuery();
        _ambulanceDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<AmbulanceModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}