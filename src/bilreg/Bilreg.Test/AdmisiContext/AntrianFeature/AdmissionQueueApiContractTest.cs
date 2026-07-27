using System.Reflection;
using Bilreg.Api.AdmisiContext.AntrianFeature;
using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Api.Controllers.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueApiContractTest
{
    private static AdmissionQueueV1Controller CreateV1Controller(
        IMediator mediator,
        AdmissionQueueApiOptions? options = null)
    {
        options ??= new AdmissionQueueApiOptions();
        var repo = new Mock<IAdmissionWorkstationRepo>();
        repo.Setup(x => x.LoadEntity(It.IsAny<IAdmissionWorkstationKey>()))
            .Returns(MayBe<AdmissionWorkstationModel>.None);
        var resolver = new AdmissionQueueWorkstationResolver(
            repo.Object,
            Options.Create(options),
            NullLogger<AdmissionQueueWorkstationResolver>.Instance);
        return new AdmissionQueueV1Controller(mediator, resolver, Options.Create(options));
    }

    [Fact]
    public void V1Controller_IsVersionedAuthenticatedAndPublishesRequiredOperations()
    {
        var type=typeof(AdmissionQueueV1Controller);
        type.GetCustomAttributes(typeof(AuthorizeAttribute),true).Should().NotBeEmpty();
        type.GetCustomAttributes(typeof(RouteAttribute),true).Cast<RouteAttribute>().Single().Template
            .Should().Be("api/v1/admission-queue");
        var methods=type.GetMethods().Select(x=>x.Name).ToArray();
        methods.Should().Contain(["Intake","BookingAssistance","Worklist","Display","Call","Recall","ReturnToWaiting","Start",
            "Withdraw","NoShow","Redirect","Established","NotEstablished","ListServicePoints","UpsertServicePoint",
            "RolloutStatus"]);
    }

    [Fact]
    public async Task RolloutStatus_DispatchesAuthenticatedPreflightQuery()
    {
        var mediator=new Mock<IMediator>();
        var response=new AdmissionQueueGetRolloutStatusResponse(
            true,[],[],true,true,true,0);
        mediator.Setup(x=>x.Send(It.IsAny<AdmissionQueueGetRolloutStatusQry>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var sut=CreateV1Controller(mediator.Object);
        var result=await sut.RolloutStatus();
        result.Should().BeOfType<OkObjectResult>();
        mediator.Verify(x=>x.Send(It.IsAny<AdmissionQueueGetRolloutStatusQry>(),It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task V1LoketMutation_RejectsUnmappedWorkstation()
    {
        var options=new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        };
        var sut=CreateV1Controller(Mock.Of<IMediator>(),options);
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L1";
        http.Request.Headers["X-Workstation-Key"]="UNKNOWN";
        sut.ControllerContext=new ControllerContext{HttpContext=http};
        var act=()=>sut.Call("Q",1,new ActorLoketBody("L1","u"));
        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task V1LoketMutation_RejectsMissingWorkstationHeader()
    {
        var options=new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        };
        var sut=CreateV1Controller(Mock.Of<IMediator>(),options);
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L1";
        sut.ControllerContext=new ControllerContext{HttpContext=http};
        var act=()=>sut.Call("Q",1,new ActorLoketBody("L1","u"));
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*X-Workstation-Key*");
    }

    [Fact]
    public async Task LegacyGate_WhenDisabled_HidesRouteWithoutDispatching()
    {
        var mediator=new Mock<IMediator>();
        var sut=new AntrianController(mediator.Object,
            Options.Create(new AdmissionQueueApiOptions{LegacyEndpointsEnabled=false}),
            NullLogger<AntrianController>.Instance);
        var result=await sut.AnonymousIntake(new LegacyAnonymousIntakeBody("ADM","Admission"));
        result.Should().BeOfType<NotFoundResult>(); mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public void CompatibilityGate_DefaultsEnabled()=>new AdmissionQueueApiOptions().LegacyEndpointsEnabled.Should().BeTrue();

    [Fact]
    public void SignalRRefresh_DefaultsEnabled()=>new AdmissionQueueApiOptions().SignalRRefreshEnabled.Should().BeTrue();

    [Fact]
    public void RefreshHub_IsAuthorizedAndExposesStablePath()
    {
        typeof(Bilreg.Api.SignalR.AdmissionQueueRefreshHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute),true).Should().NotBeEmpty();
        Bilreg.Api.SignalR.AdmissionQueueRefreshContracts.HubPath.Should().Be("/hubs/admission-queue");
        Bilreg.Api.SignalR.AdmissionQueueRefreshContracts.RefreshHintEvent.Should().Be("RefreshHint");
    }

    [Fact]
    public async Task V1LoketMutation_RejectsPayloadThatDoesNotMatchWorkstationContext()
    {
        var options=new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        };
        var sut=CreateV1Controller(Mock.Of<IMediator>(),options);
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L1";
        http.Request.Headers["X-Workstation-Key"]="ADM-01";
        sut.ControllerContext=new ControllerContext{HttpContext=http};
        var act=()=>sut.Call("Q",1,new ActorLoketBody("L2","u"));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task V1LoketMutation_RejectsWorkstationMappedToAnotherLoket()
    {
        var options=new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        };
        var sut=CreateV1Controller(Mock.Of<IMediator>(),options);
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L2";
        http.Request.Headers["X-Workstation-Key"]="ADM-01";
        sut.ControllerContext=new ControllerContext{HttpContext=http};
        var act=()=>sut.Call("Q",1,new ActorLoketBody("L2","u"));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task V1LoketMutation_DispatchesOnlyWhenConfiguredWorkstationMatchesLoket()
    {
        var mediator=new Mock<IMediator>();
        mediator.Setup(x=>x.Send(It.Is<AdmissionQueueCallCmd>(c=>c.LoketKey=="L1"),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmissionQueueOperationResponse("Q",1,"Outstanding"));
        var options=new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        };
        var sut=CreateV1Controller(mediator.Object,options);
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L1";
        http.Request.Headers["X-Workstation-Key"]="ADM-01";
        sut.ControllerContext=new ControllerContext{HttpContext=http};

        var result=await sut.Call("Q",1,new ActorLoketBody("L1","u"));

        result.Should().BeOfType<OkObjectResult>();
        mediator.Verify(x=>x.Send(It.Is<AdmissionQueueCallCmd>(c=>c.LoketKey=="L1"),It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task LegacyGate_WhenDisabled_HidesDirectStartWithoutDispatching()
    {
        var mediator=new Mock<IMediator>();
        var sut=new AntrianController(mediator.Object,
            Options.Create(new AdmissionQueueApiOptions{LegacyEndpointsEnabled=false}),
            NullLogger<AntrianController>.Instance);
        var result=await sut.Start(new AdmissionQueueStartCmd("Q",1,"u"));
        result.Should().BeOfType<NotFoundResult>(); mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public void AdmisiRajalOfficerWorklistController_IsVersionedAuthenticatedCompositionRoute()
    {
        var type=typeof(Bilreg.Api.Controllers.AdmisiContext.RegFeature.AdmisiRajalOfficerWorklistController);
        type.GetCustomAttributes(typeof(AuthorizeAttribute),true).Should().NotBeEmpty();
        type.GetCustomAttributes(typeof(RouteAttribute),true).Cast<RouteAttribute>().Single().Template
            .Should().Be("api/v1/admisi-rajal");
        type.GetMethods().Select(x=>x.Name).Should().Contain("OfficerWorklist");
    }

    [Fact]
    public async Task QueueLinkedWalkIn_UsesServerResolvedLoketBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanWalkInCommand>(c =>
                    c.AdmissionAntrianId == "Q1" &&
                    c.AdmissionNoUrut == 3 &&
                    c.AdmissionExpectedRowVersion == "AQ==" &&
                    c.AdmissionLoketKey == "L1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanCreateResponse("R1", 1));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<HttpRequest>(), null))
            .Returns(new AdmissionQueueWorkstationContext("WS1", "WS1", "L1", true, "test"));
        var sut = new RegController(mediator.Object, resolver.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var cmd = new RegJalanWalkInCommand(
            "P1", "U1", "J1", "8", "", "D1", "SV1", "08:00", "K1", "",
            "Q1", 3, "AQ==");

        var result = await sut.Save(cmd);

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyAll();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task NonQueueWalkIn_DoesNotRequireWorkstationResolution()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanWalkInCommand>(c => c.AdmissionLoketKey == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanCreateResponse("R1", 1));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);
        var cmd = new RegJalanWalkInCommand(
            "P1", "U1", "J1", "8", "", "D1", "SV1", "08:00", "K1", "");

        var result = await sut.Save(cmd);

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task QueueLinkedBooking_UsesServerResolvedLoketBeforeDispatch()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanByBookingCmd>(c =>
                    c.AdmissionAntrianId == "Q1" &&
                    c.AdmissionNoUrut == 4 &&
                    c.AdmissionExpectedRowVersion == "AQ==" &&
                    c.AdmissionLoketKey == "L1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanByBookingResponse("R1", 2));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<HttpRequest>(), null))
            .Returns(new AdmissionQueueWorkstationContext("WS1", "WS1", "L1", true, "test"));
        var sut = new RegController(mediator.Object, resolver.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var cmd = new RegJalanByBookingCmd(
            "B1", "U1", "K1", "8", "", "J1", "",
            "Q1", 4, "AQ==");

        var result = await sut.Save(cmd);

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyAll();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task NonQueueBooking_DoesNotRequireWorkstationResolution()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanByBookingCmd>(c => c.AdmissionLoketKey == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanByBookingResponse("R1", 2));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);
        var cmd = new RegJalanByBookingCmd("B1", "U1", "K1", "8", "", "J1", "");

        var result = await sut.Save(cmd);

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DirectWalkIn_DispatchesNoneBehaviorWithoutWorkstationResolution()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanWalkInCommand>(c =>
                    c.AdmissionQueueBehavior == RegistrationAdmissionQueueBehavior.None &&
                    c.AdmissionLoketKey == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanCreateResponse("R1", 1));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);

        var result = await sut.SaveDirect(new RegJalanWalkInCommand(
            "P1", "U1", "J1", "8", "", "D1", "SV1", "08:00", "K1", ""));

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DirectBooking_DispatchesNoneBehaviorWithoutWorkstationResolution()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<RegJalanByBookingCmd>(c =>
                    c.AdmissionQueueBehavior == RegistrationAdmissionQueueBehavior.None &&
                    c.AdmissionLoketKey == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegJalanByBookingResponse("R1", 2));
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);

        var result = await sut.SaveDirect(new RegJalanByBookingCmd("B1", "U1", "K1", "8", "", "J1", ""));

        result.Should().BeOfType<OkObjectResult>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DirectWalkIn_WithQueueContext_IsRejectedWithoutDispatchOrWorkstationResolution()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);
        var cmd = new RegJalanWalkInCommand(
            "P1", "U1", "J1", "8", "", "D1", "SV1", "08:00", "K1", "", "Q1", 1, "AQ==");

        var act = () => sut.SaveDirect(cmd);

        await act.Should().ThrowAsync<ArgumentException>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DirectBooking_WithQueueContext_IsRejectedWithoutDispatchOrWorkstationResolution()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var resolver = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var sut = new RegController(mediator.Object, resolver.Object);
        var cmd = new RegJalanByBookingCmd("B1", "U1", "K1", "8", "", "J1", "", "Q1", 1, "AQ==");

        var act = () => sut.SaveDirect(cmd);

        await act.Should().ThrowAsync<ArgumentException>();
        resolver.VerifyNoOtherCalls();
        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnToWaiting_DispatchesVersionedCommandForResolvedLoket()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<AdmissionQueueReturnToWaitingCmd>(c =>
                    c.AntrianId == "Q" &&
                    c.NoUrut == 1 &&
                    c.LoketKey == "L1" &&
                    c.ExpectedRowVersion.SequenceEqual(new byte[] { 1 }) &&
                    c.UserId == "u"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmissionQueueOperationResponse("Q", 1, "Waiting"));
        var options = new AdmissionQueueApiOptions
        {
            Workstations = [new() { WorkstationKey = "ADM-01", LoketKey = "L1" }]
        };
        var sut = CreateV1Controller(mediator.Object, options);
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Workstation-Key"] = "ADM-01";
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        var result = await sut.ReturnToWaiting(
            "Q",
            1,
            new VersionedActorLoketBody(null, "AQ==", "u"));

        result.Should().BeOfType<OkObjectResult>();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task AdmisiRajalOfficerWorklist_DefaultResponseRemainsLegacyArray()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<AdmisiRajalOfficerWorklistQuery>(q => !q.ActiveOnly),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmisiRajalOfficerWorklistPage([], false, null, 0));
        var sut = new Bilreg.Api.Controllers.AdmisiContext.RegFeature
            .AdmisiRajalOfficerWorklistController(mediator.Object);

        var result = await sut.OfficerWorklist(
            "2026-07-23", null, null, null, 0, 100, false, false);

        ResponseData(result).Should()
            .BeAssignableTo<IReadOnlyList<AdmisiRajalOfficerWorklistItem>>();
    }

    [Fact]
    public async Task AdmisiRajalOfficerWorklist_MetadataOptInReturnsPage()
    {
        var expected = new AdmisiRajalOfficerWorklistPage([], true, 100, 250);
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<AdmisiRajalOfficerWorklistQuery>(q =>
                    q.ActiveOnly && q.Offset == 0 && q.Limit == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var sut = new Bilreg.Api.Controllers.AdmisiContext.RegFeature
            .AdmisiRajalOfficerWorklistController(mediator.Object);

        var result = await sut.OfficerWorklist(
            "2026-07-23", null, null, null, 0, 100, true, true);

        ResponseData(result).Should().BeSameAs(expected);
    }

    private static object ResponseData(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value;
        value.Should().NotBeNull();
        var property = value!.GetType().GetProperty(
            "Data",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        property.Should().NotBeNull();
        return property!.GetValue(value)!;
    }
}
