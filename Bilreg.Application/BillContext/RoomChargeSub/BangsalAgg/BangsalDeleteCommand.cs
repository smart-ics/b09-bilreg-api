﻿using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalDeleteCommand(string BangsalId) : IRequest, IBangsalKey;

public class BangsalDeleteHandler : IRequestHandler<BangsalDeleteCommand>
{
    private readonly IBangsalWriter _writer;

    public BangsalDeleteHandler(IBangsalWriter writer)
    {
        _writer = writer;
    }
    public Task Handle(BangsalDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BangsalId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;

    }
}

public class BangsalDeleteHandlerTest
{
    private readonly Mock<IBangsalWriter> _writer;
    private BangsalDeleteHandler _sut;

    public BangsalDeleteHandlerTest()
    {
        _writer = new Mock<IBangsalWriter>();
        _sut = new BangsalDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        BangsalDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyBangsalId_ThenThrowArgumentNullException()
    {
        BangsalDeleteCommand request = new BangsalDeleteCommand(string.Empty);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}