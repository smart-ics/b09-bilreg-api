using System.Text.Json;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.Helpers;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienSetKeluargaCommand(
    string PasienId,
    string KeluargaName,
    string KeluargaRelasi,
    string KeluargaNoTelp,
    string KeluargaAlamat1,
    string KeluargaAlamat2,
    string KeluargaKota,
    string KeluargaKodePos,
    string UserId
) : IRequest, IPasienKey;

public class PasienSetKeluargaHandler : IRequestHandler<PasienSetKeluargaCommand>
{
    private readonly IFactoryLoad<PasienModel, IPasienKey> _factory;
    private readonly IPasienWriter _writer;
    
    private const string ACTIVITY_NAME = "PasienSetKeluarga";

    public PasienSetKeluargaHandler(IFactoryLoad<PasienModel, IPasienKey> factory, IPasienWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(PasienSetKeluargaCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PasienId);
        Guard.IsNotWhiteSpace(request.KeluargaName);
        Guard.IsNotWhiteSpace(request.KeluargaRelasi);
        Guard.IsNotWhiteSpace(request.KeluargaNoTelp);
        Guard.IsNotWhiteSpace(request.KeluargaAlamat1);
        Guard.IsNotWhiteSpace(request.KeluargaKota);
        Guard.IsNotWhiteSpace(request.KeluargaKodePos);

        // BUILD
        var pasien = _factory.Load(request);
        var originalPasienData = PropertyChangeHelper.CloneObject<PasienModel, PasienModelSerializable>(pasien); 
        pasien.SetKeluarga(request.KeluargaName, request.KeluargaRelasi, request.KeluargaNoTelp,
            request.KeluargaAlamat1, request.KeluargaAlamat2, request.KeluargaKota, request.KeluargaKodePos);

        var changes = PropertyChangeHelper.GetChanges(originalPasienData, pasien);
        var pasienLog = new PasienLogModel(request.PasienId, ACTIVITY_NAME, request.UserId);
        pasienLog.SetChangeLog(changes);
        pasien.Add(pasienLog);
        
        // WRITE
        _ = _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetKeluargaHandlerTest
{
    private readonly Mock<IFactoryLoad<PasienModel, IPasienKey>> _factory;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetKeluargaHandler _sut;

    public PasienSetKeluargaHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PasienModel, IPasienKey>>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetKeluargaHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        PasienSetKeluargaCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPasienId_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("", "B", "C", "D", "E", "F", "G", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaName_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "", "C", "D", "E", "F", "G", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaRelasi_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "", "D", "E", "F", "G", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaNoTelp_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "", "E", "F", "G", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaAlamat1_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "", "F", "G", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaKota_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "", "H", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaKodePos_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "G", "", "I");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidPasienId_ThenThrowKeyNotFoundException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "G", "H", "I");
        _factory.Setup(x => x.Load(It.IsAny<IPasienKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}