using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuSaveCommand(string SukuId, string SukuName) : IRequest, ISukuKey;

public class SukuSaveHandler : IRequestHandler<SukuSaveCommand>
{
    private readonly ISukuWriter _writer;

    public SukuSaveHandler(ISukuWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(SukuSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SukuId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SukuName);
        
        //  BUILD
        var suku = SukuModel.Create(request.SukuId, request.SukuName);
        
        //  SAVE
        _writer.Save(suku);
        return Task.CompletedTask;
    }
}

public class SukuSaveHandlerTest
{
    private readonly SukuSaveHandler _sut;
    private readonly Mock<ISukuWriter> _writer;

    public SukuSaveHandlerTest()
    {
        _writer = new Mock<ISukuWriter>();
        _sut = new SukuSaveHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowEx()
    {
        //  ARRANGE
        SukuSaveCommand request = null!;
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        await ex.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task  GivenSukuIdEmpty_ThenThrowEx()
    {
        //  ARRANGE
        var request = new SukuSaveCommand("", "B");
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        await ex.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenSukuNameEmpty_ThenThrowEx()
    {
        //  ARRANGE
        var request = new SukuSaveCommand("A", "");
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        await ex.Should().ThrowAsync<ArgumentException>();
    }
}