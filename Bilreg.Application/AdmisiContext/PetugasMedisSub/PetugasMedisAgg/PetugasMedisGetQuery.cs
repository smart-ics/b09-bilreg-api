using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisGetQuery(string PetugasMedisId) : IRequest<PetugasMedisGetResponse>, IPetugasMedisKey;

public record PetugasMedisSatTugasGetResponse(
    string SatTugasId,
    string SatTugasName,
    bool IsUtama
);

public record PetugasMedisLayananGetResponse(
    string LayananId,
    string LayananName
);

public record PetugasMedisGetResponse(
    string PetugasMedisId,
    string PetugasMedisName,
    string NamaSingkat,
    string SmfId,
    string SmfName,
    IEnumerable<PetugasMedisSatTugasGetResponse> ListSatTugas,
    IEnumerable<PetugasMedisLayananGetResponse> ListLayanan
);

public class PetugasMedisGetHandler : IRequestHandler<PetugasMedisGetQuery, PetugasMedisGetResponse>
{
    private readonly PetugasMedisFactory _factory;

    public PetugasMedisGetHandler(PetugasMedisFactory factory)
    {
        _factory = factory;
    }

    public async Task<PetugasMedisGetResponse> Handle(PetugasMedisGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var petugasMedis = _factory.Load(request);

        var listSatTugas = petugasMedis.ListSatTugas.Select(x =>
            new PetugasMedisSatTugasGetResponse(
                x.SatTugasId,
                x.SatTugasName,
                x.IsUtama
            ));
   
        var listLayanan = petugasMedis.ListLayanan.Select(x =>
            new PetugasMedisLayananGetResponse(
                x.LayananId,
                x.LayananName
            ));

        // RESPONSE
        var response = new PetugasMedisGetResponse(
            petugasMedis.PetugasMedisId,
            petugasMedis.PetugasMedisName,
            petugasMedis.NamaSingkat,
            petugasMedis.SmfId,
            petugasMedis.SmfName,
            listSatTugas,
            listLayanan
        );

        return response;
    }
}

public class PetugasMedisGetHandlerTest
{
    private readonly Mock<PetugasMedisFactory> _factory; 
    private readonly PetugasMedisGetHandler _sut;

    public PetugasMedisGetHandlerTest()
    {
        _factory = new Mock<PetugasMedisFactory>();
        _sut = new PetugasMedisGetHandler(_factory.Object);
    }

    [Fact]
    public async Task GivenInvalidPetugasMedisId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new PetugasMedisGetQuery("InvalidId");
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Throws<KeyNotFoundException>();

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}

