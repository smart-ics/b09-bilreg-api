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
    private readonly IPasienDal _pasienDal;
    private readonly IPasienWriter _writer;
    private readonly IPasienLogWriter _logWriter;
    
    private const string ACTIVITY_NAME = "PasienSetContact";

    public PasienSetContactHandler(IPasienDal pasienDal, IPasienWriter writer, IPasienLogWriter logWriter)
    {
        _pasienDal = pasienDal;
        _writer = writer;
        _logWriter = logWriter;
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
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id {request.PasienId} not found");

        var originalPasienData = pasien.CloneObject();
        pasien.SetContact(request.Email, request.NoTelp, request.NoHp);
        pasien.SetIdentitas(request.JenisId, request.NomorId, request.NomorKk);
        
        var changes = PropertyChangeHelper.GetChanges(originalPasienData, pasien);
        var pasienLog = new PasienLogModel(request.PasienId, ACTIVITY_NAME, request.UserId);
        pasienLog.SetChangeLog(changes);
        
        // WRITE
        _ = _writer.Save(pasien);
        _ = _logWriter.Save(pasienLog);
        return Task.CompletedTask;
    }
}

public class PasienSetContactHandlerTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly Mock<IPasienLogWriter> _logWriter;
    private readonly PasienSetContactHandler _sut;
    
    public PasienSetContactHandlerTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _writer = new Mock<IPasienWriter>();
        _logWriter = new Mock<IPasienLogWriter>();
        _sut = new PasienSetContactHandler(_pasienDal.Object, _writer.Object, _logWriter.Object);
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
    