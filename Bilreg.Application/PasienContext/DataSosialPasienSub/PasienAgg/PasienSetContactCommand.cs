using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
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
    string NomorKk) : IRequest, IPasienKey ;

public class PasienSetContactHandler : IRequestHandler<PasienSetContactCommand>
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienWriter _writer;

    public PasienSetContactHandler(IPasienDal pasienDal, IPasienWriter writer)
    {
        _pasienDal = pasienDal;
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
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id {request.PasienId} not found");
        
        pasien.SetContact(request.Email, request.NoTelp, request.NoHp);
        pasien.SetIdentitas(request.JenisId, request.NomorId, request.NomorKk);
        
        // WRITE
        _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetContactHandlerTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetContactHandler _sut;
    
    public PasienSetContactHandlerTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetContactHandler(_pasienDal.Object, _writer.Object);
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
        var request = new PasienSetContactCommand("","A","B","C","D","E","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyNomorId_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","D","","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyJenisId_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","","E","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNomorKK_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","C","D","E","");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyEmail_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","","B","C","D","E","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNoTelp_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","","C","D","E","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyNoHp_ThrowsArgumentException_Test()
    {
        var request = new PasienSetContactCommand("1","A","B","","D","E","F");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}
    