using System.Data.SqlClient;
using System.Reflection;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.ReservationFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Journey DAL integration tests. Requires env configuration and Admisi Ranap schema.
/// Seeds inside TransHelper scopes that always roll back — never void-cleans operational DBs.
/// </summary>
[Collection("JourneyDalDb")]
[Trait("Category", "JourneyDalIntegration")]
public class JourneyDalIntegrationTest
{
    public const string EnvServer = JourneyDalTestEnv.EnvServer;
    public const string EnvDatabase = JourneyDalTestEnv.EnvDatabase;
    public const string EnvUser = JourneyDalTestEnv.EnvUser;
    public const string EnvPassword = JourneyDalTestEnv.EnvPassword;

    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly JourneyDal _sut;
    private readonly IOptions<DatabaseOptions> _opt;
    private readonly string _connStr;

    public JourneyDalIntegrationTest()
    {
        ResetConnStringCache();
        var (opt, connStr) = JourneyDalTestEnv.RequireConfiguredConnection();
        _opt = opt;
        _connStr = connStr;
        JourneyDalTestEnv.RequireSchema(connStr);
        _sut = new JourneyDal(_opt, cancellationEligibility: new RegistrationCancellationEligibilityRepo(
            new RegistrationCancellationEligibilityDal(_opt)));
    }

    [Fact]
    public void MissingSchema_Is_ConfigurationError_NotSoftSkip()
    {
        // Constructor already failed if schema missing; this documents the contract.
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        var act = () => conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BILRG_AdmAdmission");
        act.Should().NotThrow();
    }

    [Fact]
    public void Admission_And_WaitingList_SameRegId_Return_OneJourney()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedWaitingList(ids.WlId1, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Waiting);

        var list = _sut.List(new JourneyListFilter(
            JourneyListScope.Active,
            SearchTerm: ids.RegId,
            PageSize: 50));

        list.Items.Count(i => i.RegId == ids.RegId).Should().Be(1);
        var item = list.Items.Single(i => i.RegId == ids.RegId);
        item.JourneyId.Should().Be($"reg:{ids.RegId}");
        item.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired);

        var detail = _sut.GetByJourneyId(item.JourneyId);
        detail.Should().NotBeNull();
        detail!.Timeline.Count(t => t.RelatedRecordId == ids.WlId1).Should().BeGreaterThan(0);
        detail.CandidateAdmisiActions.Should().NotBeNull();
        detail.AllowedAdmisiActions.Should().NotBeNull();
    }

    [Fact]
    public void Multiple_WaitingLists_SameRegId_Remain_OneJourney_Chronological()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedWaitingList(ids.WlId1, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Closed,
            at: DateTime.Now.AddHours(-2));
        SeedWaitingList(ids.WlId2, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Waiting,
            at: DateTime.Now.AddHours(-1));

        var list = _sut.List(new JourneyListFilter(SearchTerm: ids.RegId, PageSize: 50));
        list.Items.Count(i => i.RegId == ids.RegId).Should().Be(1);

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();
        var wlEvents = detail!.Timeline
            .Where(t => t.RelatedRecordId is not null
                        && (t.RelatedRecordId == ids.WlId1 || t.RelatedRecordId == ids.WlId2))
            .OrderBy(t => t.OccurredAt)
            .ToList();
        wlEvents.Select(e => e.RelatedRecordId).Should().ContainInOrder(ids.WlId1, ids.WlId2);
        detail.Information.Handover!.Summary.WaitingListCount.Should().Be(2);
    }

    [Fact]
    public void SamePatient_TwoRegIds_Return_TwoJourneys()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        var reg2 = NextId("R", 10);
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedAdmission(reg2, ids.PasienId, "-", "-");

        var list = _sut.List(new JourneyListFilter(SearchTerm: ids.PasienId, PageSize: 100));
        list.Items.Count(i => i.RegId == ids.RegId || i.RegId == reg2).Should().Be(2);
    }

    [Fact]
    public void Prospective_Opname_And_Reservation_Are_Independent()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Requested);
        SeedReservation(ids.RsvId, ids.PasienId, (int)ReservationStatusEnum.Reserved);

        var list = _sut.List(new JourneyListFilter(SearchTerm: ids.PasienId, PageSize: 100));
        list.Items.Should().Contain(i => i.JourneyId == $"opn:{ids.OpnId}"
                                         && i.Stage == JourneyOperationalStage.RegistrationRequired);
        list.Items.Should().Contain(i => i.JourneyId == $"rsv:{ids.RsvId}"
                                         && i.Stage == JourneyOperationalStage.RegistrationRequired);
    }

    [Fact]
    public void Fulfilled_Opname_Reachable_Through_Admission_JourneyId()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Fulfilled, ids.RegId);
        SeedAdmission(ids.RegId, ids.PasienId, ids.OpnId, "-");

        var list = _sut.List(new JourneyListFilter(SearchTerm: ids.RegId, PageSize: 50));
        list.Items.Should().Contain(i => i.JourneyId == $"opn:{ids.OpnId}" && i.RegId == ids.RegId);
        list.Items.Should().NotContain(i => i.JourneyId == $"reg:{ids.RegId}");

        var detail = _sut.GetByJourneyId($"opn:{ids.OpnId}");
        detail.Should().NotBeNull();
        detail!.Identity.RegId.Should().Be(ids.RegId);
        detail.Stage.Should().Be(JourneyOperationalStage.HandoverRequired);
    }

    [Fact]
    public void Realized_Reservation_Reachable_Through_Admission()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedReservation(ids.RsvId, ids.PasienId, (int)ReservationStatusEnum.Realized, ids.RegId);
        SeedAdmission(ids.RegId, ids.PasienId, "-", ids.RsvId);

        var detail = _sut.GetByJourneyId($"rsv:{ids.RsvId}");
        detail.Should().NotBeNull();
        detail!.Identity.JourneyId.Should().Be($"rsv:{ids.RsvId}");
        detail.Identity.RegId.Should().Be(ids.RegId);
    }

    [Fact]
    public void Direct_Legacy_Admission_Uses_Reg_JourneyId()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();
        detail!.Identity.JourneyId.Should().Be($"reg:{ids.RegId}");
        detail.Identity.OriginKind.Should().Be(JourneyOriginKind.DirectOrLegacyAdmission);
    }

    [Fact]
    public void WaitingList_Status_Mapping_And_Closed_Never_Completed_Or_InWard()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedWaitingList(ids.WlId1, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Closed);

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();
        detail!.Stage.Should().Be(JourneyOperationalStage.PlacementStatusUnavailable);
        detail.Stage.Should().NotBe(JourneyOperationalStage.Completed);
        detail.Stage.Should().NotBe(JourneyOperationalStage.InWard);
        detail.Information.PlacementAvailability.Status.Should()
            .Be(JourneyPlacementAvailabilityStatus.Unavailable);
    }

    [Fact]
    public void Contradictory_BothSources_NeedsReconciliation()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Fulfilled, ids.RegId);
        SeedReservation(ids.RsvId, ids.PasienId, (int)ReservationStatusEnum.Realized, ids.RegId);
        SeedAdmission(ids.RegId, ids.PasienId, ids.OpnId, ids.RsvId);

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();
        detail!.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        detail.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
    }

    [Fact]
    public void Facet_Sum_Equals_TotalMatches_And_ActiveHistory_SameShape()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Requested);
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");

        var active = _sut.List(new JourneyListFilter(
            JourneyListScope.Active,
            SearchTerm: ids.PasienId,
            PageSize: 50));
        active.StageFacets.Sum(f => f.Count).Should().Be(active.TotalMatches);
        active.ProjectionVersion.Should().Be(JourneyProjectionVersions.Release1);

        var history = _sut.List(new JourneyListFilter(
            JourneyListScope.History,
            SearchTerm: ids.PasienId,
            PageSize: 50));
        history.Items.Should().AllSatisfy(i =>
        {
            i.JourneyId.Should().NotBeNullOrWhiteSpace();
            i.ProjectionVersion.Should().Be(JourneyProjectionVersions.Release1);
            i.AsOf.Should().BeCloseTo(history.AsOf, TimeSpan.FromSeconds(5));
        });
        history.StageFacets.Sum(f => f.Count).Should().Be(history.TotalMatches);
    }

    [Fact]
    public void Cursor_Pagination_Stable_When_Timestamps_Equal()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        var reg2 = NextId("R", 10);
        var reg3 = NextId("R", 10);
        var stamp = new DateTime(2026, 7, 13, 12, 0, 0);

        SeedAdmission(ids.RegId, ids.PasienId, "-", "-", admissionDate: stamp);
        SeedAdmission(reg2, ids.PasienId, "-", "-", admissionDate: stamp);
        SeedAdmission(reg3, ids.PasienId, "-", "-", admissionDate: stamp);

        var page1 = _sut.List(new JourneyListFilter(
            SearchTerm: ids.PasienId,
            PageSize: 2));
        page1.Items.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNullOrWhiteSpace();

        var page2 = _sut.List(new JourneyListFilter(
            SearchTerm: ids.PasienId,
            PageSize: 2,
            Cursor: page1.NextCursor));
        page2.Items.Should().NotBeEmpty();
        page1.Items.Select(i => i.JourneyId)
            .Intersect(page2.Items.Select(i => i.JourneyId))
            .Should().BeEmpty();
    }

    [Fact]
    public void List_And_Detail_Do_Not_Require_TaRegistrasi2_Or_Ward()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");

        int reg2Count;
        using (var conn = new SqlConnection(_connStr))
        {
            conn.Open();
            reg2Count = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM ta_registrasi2 WHERE fs_kd_reg = @RegId",
                new { RegId = ids.RegId });
        }

        // Absence is valid for Rawat Inap.
        reg2Count.Should().Be(0);

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();
        detail!.Information.PlacementAvailability.ReasonCode.Should()
            .Be(JourneyPlacementUnavailableReasonCode.WardManagementNotImplemented);
    }

    [Fact]
    public void SqlPageStage_Matches_B1_EnrichedStage_ForRelease1Matrix()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();

        // RegistrationRequired (prospective opname)
        SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Requested);
        // WardAcceptanceRequired (admission + waiting WL)
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedWaitingList(ids.WlId1, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Waiting);
        // HandoverAccepted
        var regAccepted = NextId("R", 10);
        SeedAdmission(regAccepted, ids.PasienId, "-", "-");
        SeedWaitingList(NextId("W", 12), regAccepted, ids.PasienId, (int)WaitingListStatusEnum.Accepted);
        // Cancelled prospective
        var opnCancel = NextId("O", 12);
        SeedOpname(opnCancel, ids.PasienId, (int)OpnameRequestStatusEnum.Cancelled);

        foreach (var stage in new[]
                 {
                     JourneyOperationalStage.RegistrationRequired,
                     JourneyOperationalStage.WardAcceptanceRequired,
                     JourneyOperationalStage.HandoverAccepted,
                     JourneyOperationalStage.Cancelled
                 })
        {
            var list = _sut.List(new JourneyListFilter(
                JourneyListScope.History,
                Stage: stage,
                SearchTerm: ids.PasienId,
                PageSize: 50));

            list.Items.Should().NotBeEmpty($"expected seeded rows for stage {stage}");
            list.Items.Should().OnlyContain(i => i.Stage == stage,
                "SQL stage filter must agree with B1-enriched stage on each page item");
        }
    }

    [Fact]
    public void BatchHydration_Matches_SingleJourneyLoad_Contract()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedWaitingList(ids.WlId1, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Waiting,
            at: DateTime.Now.AddHours(-2));
        SeedWaitingList(ids.WlId2, ids.RegId, ids.PasienId, (int)WaitingListStatusEnum.Closed,
            at: DateTime.Now.AddHours(-1));

        var listItem = _sut.List(new JourneyListFilter(SearchTerm: ids.RegId, PageSize: 10))
            .Items.Single(i => i.RegId == ids.RegId);
        var singleBag = _sut.LoadFactBagForTests($"reg:{ids.RegId}");
        singleBag.Should().NotBeNull();
        var singleResolution = Release1JourneyStageResolver.Resolve(singleBag!.Facts);

        listItem.JourneyId.Should().Be(singleResolution.Identity.JourneyId);
        listItem.Stage.Should().Be(singleResolution.Stage);
        listItem.NextTask?.Owner.Domain.Should().Be(singleResolution.NextTask?.Owner.Domain);
        listItem.NextTask?.Code.Should().Be(singleResolution.NextTask?.Code);

        var detail = _sut.GetByJourneyId(listItem.JourneyId)!;
        detail.Timeline.Select(t => (t.Kind, t.RelatedRecordId))
            .Should().BeEquivalentTo(singleResolution.Timeline.Select(t => (t.Kind, t.RelatedRecordId)));
    }

    [Fact]
    public void QueryCount_Is_Constant_Across_PageSizes_1_10_50()
    {
        using var trans = TransHelper.NewScope();
        var pasienId = "P" + Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 55; i++)
            SeedAdmission(NextId("R", 10), pasienId, "-", "-");

        var counts = new List<int>();
        foreach (var pageSize in new[] { 1, 10, 50 })
        {
            _ = _sut.List(new JourneyListFilter(SearchTerm: pasienId, PageSize: pageSize));
            var diag = _sut.LastListDiagnostics;
            diag.Should().NotBeNull();
            diag!.DatabaseRoundTrips.Should().Be(JourneyDal.FixedListRoundTripsWithPage);
            counts.Add(diag.DatabaseRoundTrips);
        }

        counts.Distinct().Should().ContainSingle("round-trips must not grow with page size");
    }

    [Fact]
    public void TransactionRollback_Leaves_No_Seeded_Records()
    {
        var ids = NewIds();
        using (var trans = TransHelper.NewScope())
        {
            SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
            SeedOpname(ids.OpnId, ids.PasienId, (int)OpnameRequestStatusEnum.Requested);
            // Scope disposed without Complete → rollback.
        }

        using var conn = new SqlConnection(_connStr);
        conn.Open();
        conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@Id AND VodDate=@Vod",
                new { Id = ids.RegId, Vod = VoidSentinel })
            .Should().Be(0);
        conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM BILRG_AdmOpnameRequest WHERE OpnameRequestId=@Id AND VodDate=@Vod",
                new { Id = ids.OpnId, Vod = VoidSentinel })
            .Should().Be(0);
    }

    [Fact]
    public void CancelAdmission_NotExecutable_When_BillingItems_Exist()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");
        SeedBillingItem(ids.RegId);

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}");
        detail.Should().NotBeNull();

        detail!.CandidateAdmisiActions.Should().Contain(a =>
            a.Code == JourneyActionCode.CancelAdmission && a.CanExecute);

        detail.AllowedAdmisiActions.Should().Contain(a =>
            a.Code == JourneyActionCode.CancelAdmission
            && !a.CanExecute
            && a.BlockedReason == JourneyAllowedActionEvaluator.RegistrationHasBillingItemsBlockedReason);
    }

    [Fact]
    public void CancelAdmission_Has_No_Unimplemented_Dependency_Blockers()
    {
        using var trans = TransHelper.NewScope();
        var ids = NewIds();
        SeedAdmission(ids.RegId, ids.PasienId, "-", "-");

        var detail = _sut.GetByJourneyId($"reg:{ids.RegId}")!;
        var cancel = detail.AllowedAdmisiActions.SingleOrDefault(a => a.Code == JourneyActionCode.CancelAdmission);
        cancel.Should().NotBeNull();
        if (cancel!.BlockedReason is not null)
        {
            cancel.BlockedReason.Should().NotContain("Bed Occupancy");
            cancel.BlockedReason.Should().NotContain("Medication");
            cancel.BlockedReason.Should().NotContain("Clinical Documentation");
            cancel.BlockedReason.Should().NotContain("Ward Transfer");
        }
    }

    private void SeedBillingItem(string regId)
    {
        using (var conn = new SqlConnection(_connStr))
        {
            conn.Open();
            var billingId = ("BIL" + Guid.NewGuid().ToString("N"))[..26];
            conn.Execute(
                "INSERT INTO ta_trs_billing (fs_kd_trs, fs_kd_reg) VALUES (@billingId, @regId)",
                new { billingId, regId });
        }
    }

    private SeedIds NewIds() =>
        new(
            NextId("R", 10),
            NextId("O", 12),
            NextId("S", 12),
            NextId("W", 12),
            NextId("W", 12),
            "P" + Guid.NewGuid().ToString("N")[..8]);

    private void SeedOpname(string id, string pasienId, int status, string fulfilledRegId = "-")
    {
        var dal = new OpnameRequestDal(_opt);
        dal.Insert(new OpnameRequestDto(
            id, status, pasienId, "", "3000-01-01", "",
            "D001", "Dokter Uji", DateTime.Now, "notes", fulfilledRegId,
            "journey-it", DateTime.Now, "journey-it", DateTime.Now, "", VoidSentinel, "CH0001"));
    }

    private void SeedReservation(string id, string pasienId, int status, string realizedRegId = "-")
    {
        var dal = new ReservationDal(_opt);
        dal.Insert(new ReservationDto(
            id, status, pasienId, "Pasien Uji", new DateTime(1990, 1, 1), "L",
            DateTime.Now, "1", "Kelas 1", "B001", "Bangsal A", realizedRegId,
            "journey-it", DateTime.Now, "journey-it", DateTime.Now, "", VoidSentinel));
    }

    private void SeedAdmission(
        string regId,
        string pasienId,
        string opnameRequestId,
        string reservationId,
        DateTime? admissionDate = null)
    {
        var dal = new AdmissionDal(_opt);
        dal.Insert(new AdmissionDto(
            regId,
            (int)AdmissionStatusEnum.Admitted,
            0,
            pasienId, "", "3000-01-01", "",
            opnameRequestId, reservationId,
            "1", "Kelas 1", "B001", "Bangsal A",
            admissionDate ?? DateTime.Now,
            "journey-it", admissionDate ?? DateTime.Now,
            "journey-it", admissionDate ?? DateTime.Now,
            "", VoidSentinel));
    }

    private void SeedWaitingList(
        string wlId,
        string regId,
        string pasienId,
        int status,
        DateTime? at = null)
    {
        var dal = new WaitingListDal(_opt);
        var stamp = at ?? DateTime.Now;
        dal.Insert(new WaitingListDto(
            wlId, status, regId, pasienId, "", "3000-01-01", "",
            "1", "Kelas 1", "B001", "Bangsal A", 5,
            "journey-it", stamp, "journey-it", stamp, "", VoidSentinel));
    }

    private static string NextId(string prefix, int len)
    {
        var raw = prefix + Guid.NewGuid().ToString("N");
        return raw[..Math.Min(len, raw.Length)];
    }

    private static void ResetConnStringCache()
    {
        var field = typeof(ConnStringHelper).GetField("_connString",
            BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, string.Empty);
    }

    private sealed record SeedIds(
        string RegId,
        string OpnId,
        string RsvId,
        string WlId1,
        string WlId2,
        string PasienId);
}

[CollectionDefinition("JourneyDalDb", DisableParallelization = true)]
public class JourneyDalDbCollection : ICollectionFixture<object>;
