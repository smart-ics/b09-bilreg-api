using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public record JenisTarifDeleteCommand(string JenisTarifId) : IRequest, IJenisTarifKey;

public class JenisTarifDeleteHandler : IRequestHandler<JenisTarifDeleteCommand>
{
    private readonly IJenisTarifWriter _writer;

    public JenisTarifDeleteHandler(IJenisTarifWriter writer)
    {
        _writer = writer;
    }
    
    public Task Handle(JenisTarifDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD 
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.JenisTarifId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class JenisTarifDeleteHandlerTest
{
    private readonly Mock<IJenisTarifWriter> _writer;
    private JenisTarifDeleteHandler _sut;

    public JenisTarifDeleteHandlerTest()
    {
        _writer = new Mock<IJenisTarifWriter>();
        _sut = new JenisTarifDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        JenisTarifDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJenisTarifId_ThenThrowArgumentNullException()
    {
        JenisTarifDeleteCommand request = new JenisTarifDeleteCommand(string.Empty);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}