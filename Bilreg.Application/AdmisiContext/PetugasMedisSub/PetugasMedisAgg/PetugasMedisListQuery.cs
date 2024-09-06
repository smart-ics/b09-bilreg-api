using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisListQuery() : IRequest<IEnumerable<PetugasMedisListResponse>>;
public record PetugasMedisSatTugasListResponse(
    string PetugasMedisId,
    string SatTugasId,
    string SatTugasName,
    bool IsUtama
);

public record PetugasMedisLayananListResponse(
    string PetugasMedisId,
    string LayananId,
    string LayananName
);

public record PetugasMedisListResponse(
    string PetugasMedisId,
    string PetugasMedisName,
    string NamaSingkat,
    string SmfId,
    string SmfName,
    IEnumerable<PetugasMedisSatTugasListResponse> ListSatTugas,
    IEnumerable<PetugasMedisLayananListResponse> ListLayanan
);

public class PetugasMedisListHandler : IRequestHandler<PetugasMedisListQuery, IEnumerable<PetugasMedisListResponse>>
{
    private readonly PetugasMedisFactory _factory;
    private readonly IPetugasMedisDal _petugasMedisDal;

    public PetugasMedisListHandler(PetugasMedisFactory factory, IPetugasMedisDal petugasMedisDal)
    {
        _factory = factory;
        _petugasMedisDal = petugasMedisDal;
    }

    public Task<IEnumerable<PetugasMedisListResponse>> Handle(PetugasMedisListQuery request,
        CancellationToken cancellationToken)
    {
        // QUERY
        var listPetugasMedis = _petugasMedisDal.ListData()
            ?? throw new KeyNotFoundException("Petugas Medis not found");

        // RESPONSE
        var response = listPetugasMedis.Select(BuildPetugasMedisListItem);
        return Task.FromResult(response);
    }

    private PetugasMedisListResponse BuildPetugasMedisListItem(IPetugasMedisKey key)
    {
        var petugasMedis = _factory.Load(key);
        var listSatTugas = petugasMedis.ListSatTugas.Select(x =>
            new PetugasMedisSatTugasListResponse(x.PetugasMedisId, x.SatTugasId, x.SatTugasName, x.IsUtama));
        var listLayanan = petugasMedis.ListLayanan.Select(x =>
            new PetugasMedisLayananListResponse(x.PetugasMedisId, x.LayananId, x.LayananName));

        var response = new PetugasMedisListResponse(
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

public class PetugasMedisListHandlerTest
{
    private readonly Mock<IPetugasMedisDal> _petugasMedisDalMock;
    private readonly Mock<PetugasMedisFactory> _petugasMedisFactoryMock;
    private readonly PetugasMedisListHandler _sut;

    public PetugasMedisListHandlerTest()
    {
        _petugasMedisDalMock = new Mock<IPetugasMedisDal>();
        _petugasMedisFactoryMock = new Mock<PetugasMedisFactory>();
        _sut = new PetugasMedisListHandler(_petugasMedisFactoryMock.Object, _petugasMedisDalMock.Object);
    }

    [Fact]
    public async Task GivenNoPetugasMedis_ThenThrowKeyNotFoundException()
    {
        // Arrange
        _petugasMedisDalMock.Setup(dal => dal.ListData())
            .Returns((IEnumerable<PetugasMedisModel>)null);

        var query = new PetugasMedisListQuery();

        // Act
        Func<Task> action = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Petugas Medis not found");
    }
}
