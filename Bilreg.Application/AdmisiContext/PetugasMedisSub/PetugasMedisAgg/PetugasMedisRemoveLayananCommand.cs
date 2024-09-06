using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisRemoveLayananCommand(string PetugasMedisId, string LayananId)
    : IRequest, IPetugasMedisKey, ILayananKey;
public class PetugasMedisRemoveLayananHandler : IRequestHandler<PetugasMedisRemoveLayananCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;

    public PetugasMedisRemoveLayananHandler(IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> factory, IPetugasMedisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(PetugasMedisRemoveLayananCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.LayananId);

        // BUILD
        var petugasMedis = _factory.Load(request);
        petugasMedis.Remove((PetugasMedisLayananModel x) => x.LayananId == request.LayananId);

        // WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class PetugasMedisRemoveLayananHandlerTest
{
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly PetugasMedisRemoveLayananHandler _sut;

    public PetugasMedisRemoveLayananHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _writer = new Mock<IPetugasMedisWriter>();
        _sut = new PetugasMedisRemoveLayananHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        // ARRANGE
        PetugasMedisRemoveLayananCommand request = null;

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPetugasMedisId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveLayananCommand("", "Layanan123");

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenEmptyLayananId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveLayananCommand("PetugasMedis123", "");

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*LayananId*");
    }

    [Fact]
    public async Task GivenInvalidPetugasMedisId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveLayananCommand("InvalidPetugasMedisId", "Layanan123");
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Throws<KeyNotFoundException>();

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenValidRequest_ThenRemoveLayananAndSaveChanges()
    {
        // ARRANGE
        var request = new PetugasMedisRemoveLayananCommand("PetugasMedis123", "Layanan123");
        var petugasMedis = new Mock<PetugasMedisModel>();

        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(petugasMedis.Object);

        // ACT
        await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        petugasMedis.Verify(x => x.Remove(It.IsAny<Predicate<PetugasMedisLayananModel>>()), Times.Once);
        _writer.Verify(x => x.Save(petugasMedis.Object), Times.Once);
    }
}
