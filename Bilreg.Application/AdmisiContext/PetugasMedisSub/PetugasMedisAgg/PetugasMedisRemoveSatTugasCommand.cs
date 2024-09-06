using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisRemoveSatTugasCommand(string PetugasMedisId, string SatuanTugasId)
    : IRequest, IPetugasMedisKey, ISatuanTugasKey;
public class PetugasMedisRemoveSatTugasHandler : IRequestHandler<PetugasMedisRemoveSatTugasCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;

    public PetugasMedisRemoveSatTugasHandler(IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> factory, IPetugasMedisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(PetugasMedisRemoveSatTugasCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.SatuanTugasId);

        // BUILD
        var petugasMedis = _factory.Load(request);
        petugasMedis.Remove((PetugasMedisSatTugasModel x) => x.SatTugasId == request.SatuanTugasId);

        // WRITE
        _=_writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class PetugasMedisRemoveSatTugasHandlerTest
{
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly PetugasMedisRemoveSatTugasHandler _sut;

    public PetugasMedisRemoveSatTugasHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _writer = new Mock<IPetugasMedisWriter>();
        _sut = new PetugasMedisRemoveSatTugasHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        // ARRANGE
        PetugasMedisRemoveSatTugasCommand request = null;

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPetugasMedisId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveSatTugasCommand("", "SatTugas123");

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
        var request = new PetugasMedisRemoveSatTugasCommand("PetugasMedis123", "");

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SatuanTugasId*");
    }

    [Fact]
    public async Task GivenInvalidPetugasMedisId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveSatTugasCommand("InvalidPetugasMedisId", "SatTugas123");
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Throws<KeyNotFoundException>();

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenValidRequest_ThenRemoveSatTugasAndSaveChanges()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveSatTugasCommand("PetugasMedis123", "SatTugas123");
        var petugasMedis = new Mock<PetugasMedisModel>();

        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(petugasMedis.Object);

        // ACT
        await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        petugasMedis.Verify(x => x.Remove(It.IsAny<Predicate<PetugasMedisSatTugasModel>>()), Times.Once);
        _writer.Verify(x => x.Save(petugasMedis.Object), Times.Once);
    }
}
