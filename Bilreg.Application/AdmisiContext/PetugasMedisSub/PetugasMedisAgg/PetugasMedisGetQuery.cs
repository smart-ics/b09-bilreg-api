using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using MediatR;

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
    public Task<PetugasMedisGetResponse> Handle(PetugasMedisGetQuery request, CancellationToken cancellationToken)
    {
        var ptgMedis = _factory.Load(request);
        var listSatTugas = ptgMedis.PetugasMedisSatTugas.Select(x =>
            new PetugasMedisSatTugasGetResponse(
                x.SatTugas.SatTugasId,
                x.SatTugas.SatTugasName,
                x.IsUtama
            ));

        var listLayanan = ptgMedis.PetugasMedisLayanan.Select(x =>
            new PetugasMedisLayananGetResponse(
                x.Layanan.LayananId,
                x.Layanan.LayananName
            ));

        // RESPONSE
        var response = new PetugasMedisGetResponse(
            ptgMedis.PetugasMedisId,
            ptgMedis.PetugasMedisName,
            ptgMedis.NamaSingkat,
            ptgMedis.Smf.SmfId,
            ptgMedis.Smf.SmfName,
            listSatTugas,
            listLayanan
        );
        return Task.FromResult(response);
    }
}
//
// public class PetugasMedisGetHandlerTest
// {
//     private readonly Mock<PetugasMedisFactory> _factory; 
//     private readonly PetugasMedisGetHandler _sut;
//
//     public PetugasMedisGetHandlerTest()
//     {
//         _factory = new Mock<PetugasMedisFactory>();
//         _sut = new PetugasMedisGetHandler(_factory.Object);
//     }
//
//     [Fact]
//     public async Task GivenInvalidPetugasMedisId_ThenThrowKeyNotFoundException()
//     {
//         // ARRANGE
//         var request = new PetugasMedisGetQuery("InvalidId");
//         _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
//             .Throws<KeyNotFoundException>();
//
//         // ACT
//         var actual = async () => await _sut.Handle(request, CancellationToken.None);
//
//         // ASSERT
//         await actual.Should().ThrowAsync<KeyNotFoundException>();
//     }
// }
//
