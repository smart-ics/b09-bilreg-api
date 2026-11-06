using System.Reflection;
using Bilreg.Api.Controllers.LabContext.LabMasterFeature;
using Bilreg.Application.LabContext.LabMasterFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabMasterControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly LabMasterController _sut;

    public LabMasterControllerTest()
    {
        _sut = new LabMasterController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributesAndRoute()
    {
        var type = typeof(LabMasterController);

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/LabContext/LabMasterFeature");
    }

    [Fact]
    public void Actions_HaveExpectedHttpAttributes()
    {
        var listDisciplines = typeof(LabMasterController).GetMethod(nameof(LabMasterController.ListDisciplines));
        listDisciplines.Should().NotBeNull();
        listDisciplines!.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("disciplines");

        var saveDiscipline = typeof(LabMasterController).GetMethod(nameof(LabMasterController.SaveDiscipline));
        saveDiscipline.Should().NotBeNull();
        saveDiscipline!.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("disciplines");

        var listTestGroups = typeof(LabMasterController).GetMethod(nameof(LabMasterController.ListTestGroups));
        listTestGroups.Should().NotBeNull();
        listTestGroups!.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("test-groups");

        var saveTestGroup = typeof(LabMasterController).GetMethod(nameof(LabMasterController.SaveTestGroup));
        saveTestGroup.Should().NotBeNull();
        saveTestGroup!.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("test-groups");
    }

    [Fact]
    public async Task ListDisciplines_DispatchesQuery_ReturnsOkWithJSendOk()
    {
        var response = new List<LabDisciplineListResponse>
        {
            new("DISC-HEM", "Hematologi"),
            new("DISC-KIM", "Kimia Klinik")
        };
        _mediatorMock.Setup(m => m.Send(It.Is<LabDisciplineListQuery>(q => q.Search == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.ListDisciplines(null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task SaveDiscipline_DispatchesCmd_ReturnsOkWithJSendOk()
    {
        var cmd = new LabDisciplineSaveCmd("DISC-PAH", "Histopatologi", "admin1");
        var response = new LabDisciplineSaveResponse("DISC-PAH", "Histopatologi");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SaveDiscipline(cmd);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task ListTestGroups_DispatchesQuery_ReturnsOkWithJSendOk()
    {
        var response = new List<LabTestGroupListResponse>
        {
            new("GRP-CBC", "Darah Lengkap"),
            new("GRP-LIP", "Profil Lipid")
        };
        _mediatorMock.Setup(m => m.Send(It.Is<LabTestGroupListQuery>(q => q.Search == "Lipid"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.ListTestGroups("Lipid");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task SaveTestGroup_DispatchesCmd_ReturnsOkWithJSendOk()
    {
        var cmd = new LabTestGroupSaveCmd("GRP-HISTO", "Histopatologi Jaringan", "admin1");
        var response = new LabTestGroupSaveResponse("GRP-HISTO", "Histopatologi Jaringan");
        _mediatorMock.Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SaveTestGroup(cmd);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
    }
}
