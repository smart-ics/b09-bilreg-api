using Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisAddSatTugasCommand(string PetugasMedisId, string SatuanTugasId)
    : IRequest, IPetugasMedisKey, ISatuanTugasKey;
    
public record PetugasMedisAddSatTugasHandler : IRequestHandler<PetugasMedisAddSatTugasCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;
    private readonly ISatuanTugasDal _satuanTugasDal;

    public PetugasMedisAddSatTugasHandler(IFactoryLoad<PetugasMedisModel,
        IPetugasMedisKey> factory, ISatuanTugasDal satuanTugasDal,
        IPetugasMedisWriter writer)
    {
        _factory = factory;
        _satuanTugasDal = satuanTugasDal;
        _writer = writer;

    }

    public Task Handle(PetugasMedisAddSatTugasCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.SatuanTugasId);
        var satuanTugas = _satuanTugasDal.GetData(request)
            ?? throw new KeyNotFoundException($"Satuan Tugas {request.SatuanTugasId} not found");

        // BUILD
        var petugasMedis = _factory.Load(request);
        petugasMedis.Add(satuanTugas);

        // WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class PetugasMedisAddSatuanTugasHandlerTest
{
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<ISatuanTugasDal> _satuanTugasDal;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly PetugasMedisAddSatTugasHandler _sut;

    public PetugasMedisAddSatuanTugasHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _satuanTugasDal = new Mock<ISatuanTugasDal>();
        _writer = new Mock<IPetugasMedisWriter>();
        _sut = new PetugasMedisAddSatTugasHandler(_factory.Object, _satuanTugasDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        // ARRANGE
        PetugasMedisAddSatTugasCommand request = null;

        // ACT
        Func<Task> actual = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPetugasMedisId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new PetugasMedisAddSatTugasCommand("", "ST123");

        // ACT
        Func<Task> actual = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenEmptySatuanTugasId_ThenThrowArgumentException()
    {
        // ARRANGE
        var request = new PetugasMedisAddSatTugasCommand("PM123", "");

        // ACT
        Func<Task> actual = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SatuanTugasId*");
    }

    [Fact]
    public async Task GivenInvalidPetugasMedisId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new PetugasMedisAddSatTugasCommand("PM123", "ST123");
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Throws<KeyNotFoundException>();

        // ACT
        Func<Task> actual = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PetugasMedisId*");
    }

    [Fact]
    public async Task GivenInvalidSatuanTugasId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new PetugasMedisAddSatTugasCommand("PM123", "ST123");
        _satuanTugasDal.Setup(x => x.GetData(It.IsAny<ISatuanTugasKey>()))
            .Returns(null as SatuanTugasModel);

        // ACT
        Func<Task> actual = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await actual.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanTugasId*");
    }
}
