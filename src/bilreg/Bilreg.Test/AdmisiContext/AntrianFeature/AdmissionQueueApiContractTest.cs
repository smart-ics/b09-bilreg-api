using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueApiContractTest
{
    [Fact]
    public void V1Controller_IsVersionedAuthenticatedAndPublishesRequiredOperations()
    {
        var type=typeof(AdmissionQueueV1Controller);
        type.GetCustomAttributes(typeof(AuthorizeAttribute),true).Should().NotBeEmpty();
        type.GetCustomAttributes(typeof(RouteAttribute),true).Cast<RouteAttribute>().Single().Template
            .Should().Be("api/v1/admission-queue");
        var methods=type.GetMethods().Select(x=>x.Name).ToArray();
        methods.Should().Contain(["Intake","BookingAssistance","Worklist","Display","Call","Recall","Start",
            "Withdraw","NoShow","Redirect","Established","NotEstablished","ListServicePoints","UpsertServicePoint"]);
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
    public async Task V1LoketMutation_RejectsPayloadThatDoesNotMatchWorkstationContext()
    {
        var sut=new AdmissionQueueV1Controller(Mock.Of<IMediator>());
        var http=new DefaultHttpContext(); http.Request.Headers["X-Loket-Key"]="L1";
        sut.ControllerContext=new ControllerContext{HttpContext=http};
        var act=()=>sut.Call("Q",1,new ActorLoketBody("L2","u"));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task V1LoketMutation_RejectsWorkstationMappedToAnotherLoket()
    {
        var options=Options.Create(new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        });
        var sut=new AdmissionQueueV1Controller(Mock.Of<IMediator>(),options);
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
        var sut=new AdmissionQueueV1Controller(mediator.Object,Options.Create(new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"}]
        }));
        var http=new DefaultHttpContext();
        http.Request.Headers["X-Loket-Key"]="L1";
        http.Request.Headers["X-Workstation-Key"]="ADM-01";
        sut.ControllerContext=new ControllerContext{HttpContext=http};

        var result=await sut.Call("Q",1,new ActorLoketBody("L1","u"));

        result.Should().BeOfType<OkObjectResult>();
        mediator.Verify(x=>x.Send(It.Is<AdmissionQueueCallCmd>(c=>c.LoketKey=="L1"),It.IsAny<CancellationToken>()),Times.Once);
    }
}
