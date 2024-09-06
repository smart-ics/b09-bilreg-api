using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record SetSatTugasUtamaCommand(string PetugasMedisId, string SatuanTugasId)
    : IRequest, IPetugasMedisKey, ISatuanTugasKey;

public class SetSatTugasUtamaHandler : IRequestHandler<SetSatTugasUtamaCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;

    public SetSatTugasUtamaHandler(IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> factory, IPetugasMedisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(SetSatTugasUtamaCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.SatuanTugasId);

        // BUILD
        var petugasMedis = _factory.Load(request);
        petugasMedis.SetAsSatTugasUtama(x => x.SatTugasId == request.SatuanTugasId);

        // WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class SetSatTugasUtamaHandlerTest
{
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly SetSatTugasUtamaHandler _sut;

    public SetSatTugasUtamaHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _writer = new Mock<IPetugasMedisWriter>();
        _sut = new SetSatTugasUtamaHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        // ARRANGE
        SetSatTugasUtamaCommand request = null;

        // ACT
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPetugasMedisId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new SetSatTugasUtamaCommand("", "SatTugas123");

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
        var request = new SetSatTugasUtamaCommand("PetugasMedis123", "");

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
        var request = new SetSatTugasUtamaCommand("PetugasMedis123", "InvalidSatTugasId");
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
    public async Task GivenValidRequest_ThenSetSatTugasUtamaAndSaveChanges()
    {
        // ARRANGE
        var request = new SetSatTugasUtamaCommand("PetugasMedis123", "SatTugas123");
        var satTugas = new Mock<PetugasMedisSatTugasModel>();
        var petugasMedis = new Mock<PetugasMedisModel>();

        petugasMedis.Setup(x => x.ListSatTugas)
            .Returns(new List<PetugasMedisSatTugasModel> { satTugas.Object });

        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(petugasMedis.Object);

        // ACT
        await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        satTugas.Verify(x => x.SetUtama(), Times.Once);
        petugasMedis.Verify(x => x.SyncId(), Times.Once);
        _writer.Verify(x => x.Save(petugasMedis.Object), Times.Once);
    }
}
