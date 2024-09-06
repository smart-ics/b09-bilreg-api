using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record UnSetSatTugasUtamaCommand(string PetugasMedisId, string SatuanTugasId)
    : IRequest, IPetugasMedisKey, ISatuanTugasKey;

public class UnSetSatTugasUtamaHandler : IRequestHandler<UnSetSatTugasUtamaCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;

    public UnSetSatTugasUtamaHandler(IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> factory, IPetugasMedisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(UnSetSatTugasUtamaCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.SatuanTugasId);

        // BUILD
        var petugasMedis = _factory.Load(request);

        var satTugas = petugasMedis.ListSatTugas
          .Find(x => x.SatTugasId == request.SatuanTugasId)
          ?? throw new KeyNotFoundException($"Satuan Tugas with ID {request.SatuanTugasId} not found.");

        satTugas.UnsetUtama();


        petugasMedis.SyncId();

        // WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class UnSetSatTugasUtamaHandlerTest
{
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly UnSetSatTugasUtamaHandler _sut;

    public UnSetSatTugasUtamaHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _writer = new Mock<IPetugasMedisWriter>();
        _sut = new UnSetSatTugasUtamaHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        // ARRANGE
        UnSetSatTugasUtamaCommand request = null;

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPetugasMedisId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new UnSetSatTugasUtamaCommand("", "SatTugas123");

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenEmptySatuanTugasId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new UnSetSatTugasUtamaCommand("PetugasMedis123", "");

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SatuanTugasId*");
    }

    [Fact]
    public async Task GivenInvalidSatuanTugasId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new UnSetSatTugasUtamaCommand("PetugasMedis123", "InvalidSatTugasId");
        var petugasMedis = new Mock<PetugasMedisModel>();

        petugasMedis.Setup(x => x.ListSatTugas)
            .Returns(new List<PetugasMedisSatTugasModel>());

        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(petugasMedis.Object);

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Satuan Tugas with ID InvalidSatTugasId not found*");
    }

    [Fact]
    public async Task GivenValidRequest_ThenUnsetSatTugasUtamaAndSaveChanges()
    {
        // ARRANGE
        var request = new UnSetSatTugasUtamaCommand("PetugasMedis123", "SatTugas123");
        var satTugas = new Mock<PetugasMedisSatTugasModel>();
        var petugasMedis = new Mock<PetugasMedisModel>();

        petugasMedis.Setup(x => x.ListSatTugas)
            .Returns(new List<PetugasMedisSatTugasModel> { satTugas.Object });

        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(petugasMedis.Object);

        // ACT
        await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        satTugas.Verify(x => x.UnsetUtama(), Times.Once);
        petugasMedis.Verify(x => x.SyncId(), Times.Once);
        _writer.Verify(x => x.Save(petugasMedis.Object), Times.Once);
    }
}

