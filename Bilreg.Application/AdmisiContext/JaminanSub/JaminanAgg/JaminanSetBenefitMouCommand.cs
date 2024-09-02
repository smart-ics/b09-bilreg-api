using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSetBenefitMouCommand(string JaminanId, string BenefitMou): IRequest, IJaminanKey;

public class JaminanSetBenefitMouHandler: IRequestHandler<JaminanSetBenefitMouCommand>
{
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;

    public JaminanSetBenefitMouHandler(IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanSetBenefitMouCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.JaminanId);
        ArgumentException.ThrowIfNullOrEmpty(request.BenefitMou);
        var existingJaminan = _jaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Jaminan id {request.JaminanId} not found");
        
        // BUILD
        existingJaminan.SetBenefitMou(request.BenefitMou);
        
        // WRITE
        _writer.Save(existingJaminan);
        return Task.CompletedTask;
    }
}

public class JaminanSetBenefitMouHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanSetBenefitMouHandler _sut;

    public JaminanSetBenefitMouHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanSetBenefitMouHandler(_jaminanDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanSetBenefitMouCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetBenefitMouCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyBenefitMou_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSetBenefitMouCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidJaminanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new JaminanSetBenefitMouCommand("A", "B");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}