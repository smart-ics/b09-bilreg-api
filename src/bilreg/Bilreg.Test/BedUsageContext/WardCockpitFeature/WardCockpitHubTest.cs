using System.Reflection;
using Bilreg.Api.SignalR;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitHubTest
{
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly Mock<IGroupManager> _groupManagerMock = new();
    private readonly WardCockpitHub _sut;

    public WardCockpitHubTest()
    {
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-12345");
        _sut = new WardCockpitHub
        {
            Context = _contextMock.Object,
            Groups = _groupManagerMock.Object
        };
    }

    [Fact]
    public void Hub_ShouldHaveAuthorizeAttribute()
    {
        var authAttr = typeof(WardCockpitHub).GetCustomAttribute<AuthorizeAttribute>();
        authAttr.Should().NotBeNull();
    }

    [Fact]
    public async Task JoinWard_WithValidBangsalId_AddsConnectionToWardGroup()
    {
        // Arrange
        const string bangsalId = "BGS-01";
        const string expectedGroupName = "Ward-BGS-01";

        // Act
        await _sut.JoinWard(bangsalId);

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync("conn-12345", expectedGroupName, default),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task JoinWard_WithNullOrWhitespace_DoesNotAddToGroup(string? invalidBangsalId)
    {
        // Act
        await _sut.JoinWard(invalidBangsalId!);

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task LeaveWard_WithValidBangsalId_RemovesConnectionFromWardGroup()
    {
        // Arrange
        const string bangsalId = "BGS-02";
        const string expectedGroupName = "Ward-BGS-02";

        // Act
        await _sut.LeaveWard(bangsalId);

        // Assert
        _groupManagerMock.Verify(
            g => g.RemoveFromGroupAsync("conn-12345", expectedGroupName, default),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LeaveWard_WithNullOrWhitespace_DoesNotRemoveFromGroup(string? invalidBangsalId)
    {
        // Act
        await _sut.LeaveWard(invalidBangsalId!);

        // Assert
        _groupManagerMock.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }
}
