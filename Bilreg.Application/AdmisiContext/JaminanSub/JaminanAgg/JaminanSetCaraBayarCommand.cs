using Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSetCaraBayarCommand(string JaminanId, string CaraBayarDkId): IRequest, IJaminanKey, ICaraBayarDkKey;

public class JaminanSetCaraBayarHandler: IRequestHandler<JaminanSetCaraBayarCommand>
{
    private readonly ICaraBayarDkDal _caraBayarDkDal;
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;

    public JaminanSetCaraBayarHandler(ICaraBayarDkDal caraBayarDkDal, IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _caraBayarDkDal = caraBayarDkDal;
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanSetCaraBayarCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.JaminanId);
        ArgumentException.ThrowIfNullOrEmpty(request.CaraBayarDkId);
        var existingJaminan = _jaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");
        var caraBayarDk = _caraBayarDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Cara bayar dk id {request.CaraBayarDkId} not found");
        
        // BUILD
        existingJaminan.SetCaraBayar(caraBayarDk);
        
        // WRITE
        _writer.Save(existingJaminan);
        return Task.CompletedTask;
    }
}

public class JaminanSetCaraBayarHandlerTest
{
    private readonly Mock<ICaraBayarDkDal> _caraBayarDkDal;
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanSetCaraBayarHandler _sut;

    public JaminanSetCaraBayarHandlerTest()
    {
        _caraBayarDkDal = new Mock<ICaraBayarDkDal>();
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanSetCaraBayarHandler(_caraBayarDkDal.Object, _jaminanDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanSetCaraBayarCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetCaraBayarCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyCaraBayarDkId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetCaraBayarCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetCaraBayarCommand("A", "B");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidCaraBayarDkId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetCaraBayarCommand("A", "B");
        _caraBayarDkDal.Setup(x => x.GetData(It.IsAny<ICaraBayarDkKey>()))
            .Returns(null as CaraBayarDkModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}