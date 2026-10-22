using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class RoomChargeCompletenessGuardTest
{
    private const string RegId = "REG-RAWAT-001";

    private static readonly DateTime AdmissionDate = new(2026, 6, 15, 8, 0, 0);
    private static readonly DateTime DischargeDate = new(2026, 6, 17, 10, 0, 0);

    private readonly Mock<IRoomChargeCompletenessQuery> _roomChargeQuery = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();

    public RoomChargeCompletenessGuardTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    #region Domain validation

    [Fact]
    public void P2_S03_01_GivenStayWithoutBilledRoomCharge_WhenValidateRoomChargeCompleteness_ThenShouldBeIncomplete()
    {
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, DischargeDate));
        SetupBilledDates();

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeFalse();
        result.Status.Should().Be(FinancialVerificationStatusEnum.CompletenessIncomplete);
        result.RequiredStayDates.Should().HaveCount(2);
        result.MissingStayDates.Should().HaveCount(2);
        result.MissingStayDates.Should().Contain(new DateTime(2026, 6, 15));
        result.MissingStayDates.Should().Contain(new DateTime(2026, 6, 16));
    }

    [Fact]
    public void P2_S03_02_GivenAllStayDaysBilled_WhenValidateRoomChargeCompleteness_ThenShouldBeComplete()
    {
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, DischargeDate));
        SetupBilledDates(new DateTime(2026, 6, 15), new DateTime(2026, 6, 16));

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeTrue();
        result.Status.Should().Be(FinancialVerificationStatusEnum.Valid);
        result.RequiredStayDates.Should().HaveCount(2);
        result.MissingStayDates.Should().BeEmpty();
    }

    [Fact]
    public void P2_S03_03_GivenNoBedOccupancy_WhenValidateRoomChargeCompleteness_ThenShouldBeNotApplicable()
    {
        _roomChargeQuery
            .Setup(q => q.GetStayInterval(RegId))
            .Returns((InpatientStayInterval?)null);

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeTrue();
        result.Status.Should().Be(FinancialVerificationStatusEnum.Valid);
        result.RequiredStayDates.Should().BeEmpty();
        result.MissingStayDates.Should().BeEmpty();
    }

    [Fact]
    public void P2_S03_04_GivenAdmissionAfterCutOff_WhenValidateRoomChargeCompleteness_ThenAdmissionDayIsNotRequired()
    {
        var admissionAfterCutOff = new DateTime(2026, 6, 15, 21, 0, 0);
        SetupStay(new InpatientStayInterval(RegId, admissionAfterCutOff, new DateTime(2026, 6, 17, 9, 0, 0)));
        SetupBilledDates(new DateTime(2026, 6, 16));

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeTrue();
        result.RequiredStayDates.Should().HaveCount(1);
        result.RequiredStayDates.Should().Contain(new DateTime(2026, 6, 16));
    }

    [Fact]
    public void P2_S03_05_GivenCheckoutBeyondGracePeriod_WhenValidateRoomChargeCompleteness_ThenDischargeDayIsRequired()
    {
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, new DateTime(2026, 6, 17, 15, 0, 0)));
        SetupBilledDates(new DateTime(2026, 6, 15), new DateTime(2026, 6, 16));

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeFalse();
        result.RequiredStayDates.Should().HaveCount(3);
        result.MissingStayDates.Should().Contain(new DateTime(2026, 6, 17));
    }

    [Fact]
    public void P2_S03_06_GivenBedNotReleased_WhenValidateRoomChargeCompleteness_ThenShouldBeIncomplete()
    {
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, null));
        SetupBilledDates(new DateTime(2026, 6, 15), new DateTime(2026, 6, 16));

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeFalse();
        result.Status.Should().Be(FinancialVerificationStatusEnum.CompletenessIncomplete);
    }

    [Fact]
    public void P2_S03_07_GivenSameDayStay_WhenValidateRoomChargeCompleteness_ThenAdmissionDayIsRequired()
    {
        var admission = new DateTime(2026, 6, 15, 21, 0, 0);
        var discharge = new DateTime(2026, 6, 15, 23, 0, 0);
        SetupStay(new InpatientStayInterval(RegId, admission, discharge));
        SetupBilledDates(new DateTime(2026, 6, 15));

        var result = CreateService().ValidateRoomChargeCompleteness(RegId);

        result.IsComplete.Should().BeTrue();
        result.RequiredStayDates.Should().HaveCount(1);
        result.RequiredStayDates.Should().Contain(new DateTime(2026, 6, 15));
    }

    [Fact]
    public void P2_S03_08_GivenCallerInterval_WhenValidateRoomChargeCompleteness_ThenShouldCompareWithBilledRoomCharge()
    {
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, DischargeDate));
        SetupBilledDates(new DateTime(2026, 6, 15), new DateTime(2026, 6, 16));

        var result = CreateService().ValidateRoomChargeCompleteness(
            RegId,
            AdmissionDate,
            DischargeDate);

        result.IsComplete.Should().BeTrue();
        result.MissingStayDates.Should().BeEmpty();
    }

    #endregion

    #region Finalize guard

    [Fact]
    public async Task P2_S03_09_GivenIncompleteRoomCharge_WhenFinalize_ThenShouldThrowBusinessValidationException()
    {
        var tataRekening = CreateAllocatedTataRekening();
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, DischargeDate));
        SetupBilledDates(new DateTime(2026, 6, 15));
        SetupTataRekeningLoad(tataRekening);

        var act = () => CreateHandler().Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "UserId", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*sewa kamar*");
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
        _tataRekeningRepo.Verify(
            r => r.SaveChanges(It.IsAny<TataRekeningModel>()),
            Times.Never);
    }

    [Fact]
    public async Task P2_S03_10_GivenCompleteRoomCharge_WhenFinalize_ThenStatusFinalized()
    {
        var tataRekening = CreateAllocatedTataRekening();
        SetupStay(new InpatientStayInterval(RegId, AdmissionDate, DischargeDate));
        SetupBilledDates(new DateTime(2026, 6, 15), new DateTime(2026, 6, 16));
        SetupTataRekeningLoad(tataRekening);

        var result = await CreateHandler().Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "UserId", null),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        _tataRekeningRepo.Verify(
            r => r.SaveChanges(It.IsAny<TataRekeningModel>()),
            Times.Once);
    }

    #endregion

    private FinancialVerificationDomainService CreateService() =>
        new(_roomChargeQuery.Object);

    private FinalizeFinancialResponsibilityHandler CreateHandler() =>
        new(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _unitOfWork.Object,
            new FixedCurrentUserContext(),
            TestTglJamProvider.Instance,
            CreateService());

    private static TataRekeningModel CreateAllocatedTataRekening()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 75_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 75_000m, 0m)]);
        return tataRekening;
    }

    private void SetupStay(InpatientStayInterval stay)
    {
        _roomChargeQuery
            .Setup(q => q.GetStayInterval(RegId))
            .Returns(stay);
    }

    private void SetupBilledDates(params DateTime[] dates)
    {
        _roomChargeQuery
            .Setup(q => q.ListBilledRoomChargeDates(RegId))
            .Returns(dates);
    }

    private void SetupTataRekeningLoad(TataRekeningModel tataRekening)
    {
        _tataRekeningRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));
    }
}
