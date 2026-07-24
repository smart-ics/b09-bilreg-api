using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class RegistrationOutcomeTest
{
    [Fact]
    public void OutcomeCombinations_AreStrict()
    {
        RegistrationOutcomeModel.Established("Q",1,"RG1","u",DateTime.Now).ReasonCode.Should().BeEmpty();
        RegistrationOutcomeModel.NotEstablished("Q",1,"DECLINED","u",DateTime.Now).RegId.Should().BeEmpty();
        FluentActions.Invoking(()=>RegistrationOutcomeModel.Established("Q",1,"","u",DateTime.Now)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(()=>RegistrationOutcomeModel.NotEstablished("Q",1,"","u",DateTime.Now)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PassThroughCatalog_AcceptsArbitraryNonEmptyAndRejectsWhitespace()
    {
        var catalog = new PassThroughRegistrationOutcomeReasonCatalog();
        FluentActions.Invoking(() => catalog.EnsureAccepted("OPS-ARBITRARY")).Should().NotThrow();
        FluentActions.Invoking(() => catalog.EnsureAccepted("ANY-OTHER-CODE")).Should().NotThrow();
        FluentActions.Invoking(() => catalog.EnsureAccepted("")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => catalog.EnsureAccepted("  ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task AnonymousNotEstablished_FinalizesWithoutTrackerDependency()
    {
        var queues=new Mock<IAntrianRepo>(); var operations=new Mock<IRegistrationOutcomeOperationRepo>();
        var publisher=new Mock<IAdmissionQueueRefreshPublisher>();
        queues.Setup(x=>x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(InServiceQueue()));
        operations.Setup(x=>x.TryFinalize(It.Is<RegistrationOutcomeModel>(o=>
            o.OutcomeType==RegistrationOutcomeTypeEnum.NotEstablished && o.ReasonCode=="OPS-CODE"),
            "L1",It.IsAny<byte[]>(),TestTglJamProvider.Instance.Now)).Returns(true);
        var sut=new FinalizeRegistrationNotEstablishedHandler(queues.Object,operations.Object,
            TestTglJamProvider.Instance,publisher.Object,new PassThroughRegistrationOutcomeReasonCatalog());

        var result=await sut.Handle(new("Q",1,"L1",[1],"OPS-CODE","u"),default);

        result.Status.Should().Be("Done");
        operations.VerifyAll(); publisher.Verify(x=>x.PublishAsync("L1",It.IsAny<CancellationToken>()),Times.Once);
        typeof(FinalizeRegistrationNotEstablishedHandler).GetConstructors().Single().GetParameters()
            .Select(x=>x.ParameterType.Name).Should().NotContain("IPasienTrackerRepo");
        typeof(FinalizeRegistrationNotEstablishedHandler).GetConstructors().Single().GetParameters()
            .Select(x=>x.ParameterType).Should().Contain(typeof(IRegistrationOutcomeReasonCatalog));
    }

    [Fact]
    public async Task DuplicateOrConcurrentFinalization_IsConflictAndDoesNotPublish()
    {
        var queues=new Mock<IAntrianRepo>();var operations=new Mock<IRegistrationOutcomeOperationRepo>();
        var publisher=new Mock<IAdmissionQueueRefreshPublisher>();
        queues.Setup(x=>x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(InServiceQueue()));
        operations.Setup(x=>x.TryFinalize(It.IsAny<RegistrationOutcomeModel>(),"L1",It.IsAny<byte[]>(),It.IsAny<DateTime>())).Returns(false);
        var sut=new FinalizeRegistrationNotEstablishedHandler(queues.Object,operations.Object,TestTglJamProvider.Instance,publisher.Object,
            new PassThroughRegistrationOutcomeReasonCatalog());

        var act=()=>sut.Handle(new("Q",1,"L1",[1],"OPS-CODE","u"),default);
        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();
        publisher.Verify(x=>x.PublishAsync(It.IsAny<string>(),It.IsAny<CancellationToken>()),Times.Never);
    }

    [Fact]
    public async Task RecoverableValidationFailure_DoesNotCreateFinalOutcome()
    {
        var operations=new Mock<IRegistrationOutcomeOperationRepo>();
        var sut=new FinalizeRegistrationNotEstablishedHandler(Mock.Of<IAntrianRepo>(),operations.Object,
            TestTglJamProvider.Instance,Mock.Of<IAdmissionQueueRefreshPublisher>(),
            new PassThroughRegistrationOutcomeReasonCatalog());
        var act=()=>sut.Handle(new("Q",1,"L1",[1],"","u"),default);
        await act.Should().ThrowAsync<ArgumentException>();
        operations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CatalogRejection_ShortCircuitsBeforeFinalize()
    {
        var operations=new Mock<IRegistrationOutcomeOperationRepo>(MockBehavior.Strict);
        var catalog=new Mock<IRegistrationOutcomeReasonCatalog>(MockBehavior.Strict);
        catalog.Setup(x=>x.EnsureAccepted("BLOCKED")).Throws(new ArgumentException("ReasonCode is not in the approved catalog."));
        var sut=new FinalizeRegistrationNotEstablishedHandler(Mock.Of<IAntrianRepo>(),operations.Object,
            TestTglJamProvider.Instance,Mock.Of<IAdmissionQueueRefreshPublisher>(),catalog.Object);

        var act=()=>sut.Handle(new("Q",1,"L1",[1],"BLOCKED","u"),default);
        await act.Should().ThrowAsync<ArgumentException>();
        catalog.Verify(x=>x.EnsureAccepted("BLOCKED"),Times.Once);
        operations.VerifyNoOtherCalls();
    }

    private static AntrianModel InServiceQueue()
    {
        var e=AntrianEntryModel.Create(1,PersonType.Default,PasienTrackerModel.Key("-"),"","",TestTglJamProvider.Instance.Now);
        e.Serve(TestTglJamProvider.Instance.Now);
        return new("Q",new(2026,7,23),TimeOnly.MinValue,TimeOnly.MaxValue,"tag","Admission",
            new("ADM","Admission"),[e],Mock.Of<ISequencer>(),"A");
    }
}
