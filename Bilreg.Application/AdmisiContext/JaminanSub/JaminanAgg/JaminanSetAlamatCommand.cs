using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSetAlamatCommand(string JaminanId, string Alamat1, string Alamat2, string Kota): IRequest, IJaminanKey;

public class JaminanSetAlamatHandler: IRequestHandler<JaminanSetAlamatCommand>
{
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;

    public JaminanSetAlamatHandler(IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanSetAlamatCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.JaminanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Alamat1);
        var existingJaminan = _jaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");
        
        // BUILD
        existingJaminan.SetAlamat(request.Alamat1, request.Alamat2, request.Kota);
        
        // WRITE
        _writer.Save(existingJaminan);
        return Task.CompletedTask;
    }
}

public class JaminanSetAlamatHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanSetAlamatHandler _sut;

    public JaminanSetAlamatHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanSetAlamatHandler(_jaminanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanSetAlamatCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetAlamatCommand("", "B", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAlamat1_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetAlamatCommand("A", "", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetAlamatCommand("A", "B", "C", "D");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}