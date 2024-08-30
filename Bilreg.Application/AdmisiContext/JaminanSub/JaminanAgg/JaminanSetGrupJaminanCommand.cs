using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSetGrupJaminanCommand(string JaminanId, string GrupJaminanId): IRequest, IJaminanKey, IGrupJaminanKey;

public class JaminanSetGrupJaminanHandler: IRequestHandler<JaminanSetGrupJaminanCommand>
{
    private readonly IGrupJaminanDal _grupJaminanDal;
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;

    public JaminanSetGrupJaminanHandler(IGrupJaminanDal grupJaminanDal, IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _grupJaminanDal = grupJaminanDal;
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanSetGrupJaminanCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.JaminanId);
        ArgumentException.ThrowIfNullOrEmpty(request.GrupJaminanId);
        var existingJaminan = _jaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");
        var grupJaminan = _grupJaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Grup jaminan id {request.GrupJaminanId} not found");
        
        // BUILD
        existingJaminan.SetGrupJaminan(grupJaminan);
        
        // WRITE
        _writer.Save(existingJaminan);
        return Task.CompletedTask;
    }
}

public class JaminanSetGrupJaminanHandlerTest
{
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanSetGrupJaminanHandler _sut;

    public JaminanSetGrupJaminanHandlerTest()
    {
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanSetGrupJaminanHandler(_grupJaminanDal.Object, _jaminanDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanSetGrupJaminanCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetGrupJaminanCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetGrupJaminanCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetGrupJaminanCommand("A", "B");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidGrupJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetGrupJaminanCommand("A", "B");
        _grupJaminanDal.Setup(x => x.GetData(It.IsAny<IGrupJaminanKey>()))
            .Returns(null as GrupJaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}