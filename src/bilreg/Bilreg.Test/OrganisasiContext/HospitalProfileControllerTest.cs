using System.Reflection;
using Bilreg.Api.Controllers.OrganisasiContext;
using Bilreg.Application.Shared.HospitalProfileFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.OrganisasiContext;

public class HospitalProfileControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly HospitalProfileController _sut;

    public HospitalProfileControllerTest()
    {
        _sut = new HospitalProfileController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedRoute()
    {
        var type = typeof(HospitalProfileController);
        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/organisasi");
    }

    [Fact]
    public async Task GetHospitalProfile_ReturnsOkWithJSendOk()
    {
        // Arrange
        var response = new HospitalProfileResponse("1000", "RS SIMRS NEO", "Jl. Sehat", "Surabaya", "031", "info@rs.com");
        _mediatorMock.Setup(m => m.Send(It.IsAny<HospitalProfileGetQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetHospitalProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
    }
}
