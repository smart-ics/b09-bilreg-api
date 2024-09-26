using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.Helpers;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.ValidationHelper;
using Xunit;


namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienSetContactCommand(
    string PasienId, 
    string Email,
    string NoTelp,
    string NoHp,
    string JenisId, 
    string NomorId , 
    string NomorKk,
    string UserId) : IRequest, IPasienKey;

public class PasienSetContactHandler : IRequestHandler<PasienSetContactCommand>
{
    private readonly IFactoryLoad<PasienModel, IPasienKey> _factory;
    private readonly IPasienWriter _writer;
    
    private const string ACTIVITY_NAME = "PasienSetContact";

    public PasienSetContactHandler(IFactoryLoad<PasienModel, IPasienKey> factory, IPasienWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }
    
    public Task Handle(PasienSetContactCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.PasienId);
        Guard.IsNotNullOrWhiteSpace(request.JenisId);
        Guard.IsNotNullOrWhiteSpace(request.NomorId);
        Guard.IsNotEmpty(request.NomorKk);
        Guard.IsNotEmpty(request.Email);
        Guard.IsNotEmpty(request.NoTelp);
        Guard.IsNotEmpty(request.NoHp);
        
        // BUILD
        var pasien = _factory.Load(request);
        var originalPasienData = PropertyChangeHelper.CloneObject<PasienModel, PasienModelSerializable>(pasien); 
        pasien.SetContact(request.Email, request.NoTelp, request.NoHp);
        pasien.SetIdentitas(request.JenisId, request.NomorId, request.NomorKk);
        
        var changes = PropertyChangeHelper.GetChanges(originalPasienData, pasien);
        var pasienLog = new PasienLogModel(request.PasienId, ACTIVITY_NAME, request.UserId);
        pasienLog.SetChangeLog(changes);
        pasien.Add(pasienLog);
        
        // WRITE
        _ = _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetContactHandlerTest
{
    private readonly Mock<IFactoryLoad<PasienModel, IPasienKey>> _factory;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetContactHandler _sut;
    
    public PasienSetContactHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PasienModel, IPasienKey>>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetContactHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThrowsArgumentNullException_Test()
    {
        PasienSetContactCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyId_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("","A","B","C","D","E","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyNomorId_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","D","","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyJenisId_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","","E","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNomorKK_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","D","E","", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyEmail_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","","B","C","D","E","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNoTelp_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","","C","D","E","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNoHp_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","","D","E","F", "G");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}
    