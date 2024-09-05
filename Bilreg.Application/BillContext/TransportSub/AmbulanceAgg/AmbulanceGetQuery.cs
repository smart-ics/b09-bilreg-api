using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceGetQuery(string AmbulanceId) : IRequest<AmbulanceGetResponse>, IAmbulanceKey;

public record AmbulanceKomponenGetResponse(
    string AmbulanceId,
    string KomponenId,
    decimal NilaiTarif,
    bool IsTetap
);

public record AmbulanceGetResponse(
    string AmbulanceId,
    string AmbulanceName,
    bool IsAktif,
    decimal Abonement,
    IEnumerable<AmbulanceKomponenGetResponse> ListKomponen
);

public class AmbulanceGetHandler : IRequestHandler<AmbulanceGetQuery, AmbulanceGetResponse>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;

    public AmbulanceGetHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory)
    {
        _factory = factory;
    }

    public Task<AmbulanceGetResponse> Handle(AmbulanceGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var ambulance = _factory.Load(request);

        // RESPONSE
        var listKomponen = ambulance.ListKomponen.Select(x =>
            new AmbulanceKomponenGetResponse(x.AmbulanceId, x.KomponenId, x.NilaiTarif, x.IsTetap));
        var response = new AmbulanceGetResponse(ambulance.AmbulanceId, ambulance.AmbulanceName, ambulance.IsAktif,
            ambulance.Abonement, listKomponen);
        return Task.FromResult(response);
    }
}

public class AmbulanceGetHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly AmbulanceGetHandler _sut;

    public AmbulanceGetHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _sut = new AmbulanceGetHandler(_factory.Object);
    }

    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceGetQuery("A");
        _factory.Setup(x => x.Load(It.IsAny<IAmbulanceKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}