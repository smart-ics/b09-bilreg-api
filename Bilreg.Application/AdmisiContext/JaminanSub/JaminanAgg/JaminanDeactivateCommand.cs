using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanDeactivateCommand(string JaminanId): IRequest, IJaminanKey;

public class JaminanDeactivateHandler: IRequestHandler<JaminanDeactivateCommand>
{
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;

    public JaminanDeactivateHandler(IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request); 
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JaminanId);
        var existingJaminan = _jaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");
         
        // BUILD
        existingJaminan.UnSetAktif();
         
        // WRITE
        _writer.Save(existingJaminan);
        return Task.CompletedTask;
    }
}

public class JaminanDeactivateHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanDeactivateHandler _sut;

    public JaminanDeactivateHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanDeactivateHandler(_jaminanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanDeactivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanDeactivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanDeactivateCommand("A");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new JaminanDeactivateCommand("A");
        var expected = JaminanModel.Create("A", "B");
        expected.UnSetAktif();
        JaminanModel actual = null;
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(expected);
        
        _writer.Setup(x => x.Save(It.IsAny<JaminanModel>()))
            .Callback<JaminanModel>(k => actual = k);
        
        await _sut.Handle(request, CancellationToken.None);
        actual?.IsAktif.Should().BeFalse();
    }
}