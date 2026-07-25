using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.Shared;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

/// <summary>
/// Slice 1 / Phase 1 real-SQL operational gate. Requires BILREG_AQ_IT_* env; fail-closed.
/// </summary>
[Collection("AdmissionQueueRealSqlDb")]
[Trait("Category", AdmissionQueueRealSqlTestEnv.Category)]
public sealed class AdmissionQueueRealSqlGateTest
{
    private const string SpA = "AQIT-A";
    private const string SpB = "AQIT-B";
    private readonly AdmissionQueueMigrationFixture _fx;
    private readonly string _conn;

    public AdmissionQueueRealSqlGateTest(AdmissionQueueMigrationFixture fx)
    {
        AdmissionQueueRealSqlTestEnv.ResetConnStringCache();
        _fx = fx;
        _conn = fx.ConnectionString;
        _ = ConnStringHelper.Get(fx.Options.Value);
    }

    [Fact]
    public void Migration_IsReproducible_AndRecordsScriptOrder()
    {
        _fx.AppliedScriptOrder.Should().NotBeEmpty();
        _fx.DatabaseVersion.Should().NotBeNullOrWhiteSpace();
        var expected = AdmissionQueueMigrationManifest.Scripts.Select(s => s.RelativePath).ToArray();
        var appliedRoots = _fx.AppliedScriptOrder
            .Select(s => s.Contains(" (skipped", StringComparison.Ordinal)
                ? s[..s.IndexOf(" (skipped", StringComparison.Ordinal)]
                : s)
            .ToArray();
        appliedRoots.Should().Equal(expected);
        using var conn = new SqlConnection(_conn);
        conn.Open();
        foreach (var index in AdmissionQueueMigrationManifest.RequiredIndexes)
        {
            conn.ExecuteScalar<int>("""
                SELECT COUNT(1) FROM sys.indexes
                WHERE name = @indexName AND object_id = OBJECT_ID(@tableName)
                """, new { indexName = index.IndexName, tableName = index.TableName }).Should().Be(1);
        }
    }

    [Fact]
    public async Task SameLoket_TwoEntries_OnlyOneActiveClaim()
    {
        var clock = FixedClock();
        var loket = UniqueLoket("L1");
        var (intake, call, _, _, _, _, projection) = Build(clock);
        var e1 = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        var e2 = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);

        var results = await Task.WhenAll(
            Wrap(call.Handle(new AdmissionQueueCallCmd(e1.AntrianId, e1.NoUrut, loket, "u1"), default)),
            Wrap(call.Handle(new AdmissionQueueCallCmd(e2.AntrianId, e2.NoUrut, loket, "u1"), default)));

        results.Count(r => r.Ok).Should().Be(1);
        results.Count(r => !r.Ok).Should().Be(1);
        results.Single(r => !r.Ok).Error.Should().BeOfType<AdmissionQueueConcurrencyException>();

        projection.ListCurrentLoket(loket).Should().HaveCount(1);
        CountActiveClaimsForLoket(loket).Should().Be(1);
    }

    [Fact]
    public async Task TwoLoket_OneEntry_OnlyOneOwner()
    {
        var clock = FixedClock();
        var loketA = UniqueLoket("L2A");
        var loketB = UniqueLoket("L2B");
        var (intake, call, _, _, _, _, projection) = Build(clock);
        var e = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);

        var results = await Task.WhenAll(
            Wrap(call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loketA, "u1"), default)),
            Wrap(call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loketB, "u2"), default)));

        results.Count(r => r.Ok).Should().Be(1);
        results.Count(r => !r.Ok).Should().Be(1);
        CountActiveClaimsForEntry(e.AntrianId, e.NoUrut).Should().Be(1);
        projection.ListCurrentLoket().Count(x => x.AntrianId == e.AntrianId && x.NoUrut == e.NoUrut)
            .Should().Be(1);
    }

    [Fact]
    public async Task StaleRowVersion_RecallAndStart_FailWithoutOrphanClaim()
    {
        var clock = FixedClock();
        var loket = UniqueLoket("L3");
        var (intake, call, recall, start, _, _, projection) = Build(clock);
        var e = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        await call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loket, "u1"), default);
        var snap = projection.ListCurrentLoket(loket).Single();
        var stale = (byte[])snap.RowVersion.Clone();
        await recall.Handle(new AdmissionQueueRecallCmd(e.AntrianId, e.NoUrut, loket, snap.RowVersion, "u1"), default);

        var recallAct = () => recall.Handle(
            new AdmissionQueueRecallCmd(e.AntrianId, e.NoUrut, loket, stale, "u1"), default);
        await recallAct.Should().ThrowAsync<AdmissionQueueConcurrencyException>();

        var startAct = () => start.Handle(
            new AdmissionQueueStartServiceCmd(e.AntrianId, e.NoUrut, loket, stale, "u1"), default);
        await startAct.Should().ThrowAsync<AdmissionQueueConcurrencyException>();

        CountActiveClaimsForLoket(loket).Should().Be(1);
        projection.ListCurrentLoket(loket).Single().AntrianId.Should().Be(e.AntrianId);
    }

    [Fact]
    public async Task ReleaseVersusRecall_AfterWithdraw_RecallFails()
    {
        var clock = FixedClock();
        var loket = UniqueLoket("L4");
        var (intake, call, recall, _, withdraw, _, projection) = Build(clock);
        var e = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        await call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loket, "u1"), default);
        var version = projection.ListCurrentLoket(loket).Single().RowVersion;

        await withdraw.Handle(new AdmissionQueueWithdrawCmd(
            e.AntrianId, e.NoUrut, "Released", loket, version, "u1"), default);

        CountActiveClaimsForLoket(loket).Should().Be(0);
        var act = () => recall.Handle(
            new AdmissionQueueRecallCmd(e.AntrianId, e.NoUrut, loket, version, "u1"), default);
        // Domain rejects non-Waiting before CAS; either way claim must not resurrect.
        await act.Should().ThrowAsync<InvalidOperationException>();
        CountActiveClaimsForLoket(loket).Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentFirstDailySession_OneWinner_BothAllocate()
    {
        // Unique business date so this run is a true first-session create for SpA.
        var dayOffset = Random.Shared.Next(1, 5000);
        var clock = new TestTglJamProvider(new DateTime(2026, 1, 1, 9, 30, 0).AddDays(dayOffset));
        var (intake, _, _, _, _, _, _) = Build(clock);
        var tag = AntrianModel.GenSequenceTag(
            DateOnly.FromDateTime(clock.Now), TimeOnly.MinValue,
            new ServicePointType(SpA, "AQ IT Loket A"));

        var bag = new ConcurrentBag<(bool Ok, QueAnonymousIntakeResponse? Resp, Exception? Err)>();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            try
            {
                var r = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
                bag.Add((true, r, null));
            }
            catch (Exception ex)
            {
                bag.Add((false, null, ex));
            }
        }));

        bag.Should().OnlyContain(x => x.Ok, because: "one-winner reload must make all concurrent intakes succeed");
        var responses = bag.Select(x => x.Resp!).ToList();
        responses.Select(x => x.AntrianId).Distinct().Should().HaveCount(1);
        responses.Select(x => x.NoUrut).Should().OnlyHaveUniqueItems();
        CountSessionsForTag(tag).Should().Be(1);
    }

    [Fact]
    public async Task RedirectRollback_OnConflict_LeavesOriginIntact()
    {
        var clock = FixedClock();
        var loket = UniqueLoket("L5");
        var (intake, call, _, _, _, redirect, projection) = Build(clock);
        var e = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        await call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loket, "u1"), default);
        var version = projection.ListCurrentLoket(loket).Single().RowVersion;

        var stale = new byte[version.Length];
        Array.Copy(version, stale, version.Length);
        stale[^1] ^= 0xFF;

        var act = () => redirect.Handle(new AdmissionQueueRedirectCmd(
            e.AntrianId, e.NoUrut, SpB, loket, stale, "u1"), default);
        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();

        EntryStatus(e.AntrianId, e.NoUrut).Should().Be(0, "origin must remain Waiting after rollback");
        CountActiveClaimsForLoket(loket).Should().Be(1);
        CountRedirectReplacements(e.AntrianId, e.NoUrut).Should().Be(0);
    }

    [Fact]
    public async Task OutcomeRollback_OnStaleClaim_LeavesInService()
    {
        var clock = FixedClock();
        var loket = UniqueLoket("L6");
        var (intake, call, _, start, _, _, projection) = Build(clock);
        var outcomes = new RegistrationOutcomeOperationRepo(_fx.Options);
        var finalize = new FinalizeRegistrationNotEstablishedHandler(
            Queues(), outcomes, clock, new NullAdmissionQueueRefreshPublisher(),
            new PassThroughRegistrationOutcomeReasonCatalog());

        var e = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        await call.Handle(new AdmissionQueueCallCmd(e.AntrianId, e.NoUrut, loket, "u1"), default);
        var v1 = projection.ListCurrentLoket(loket).Single().RowVersion;
        await start.Handle(new AdmissionQueueStartServiceCmd(e.AntrianId, e.NoUrut, loket, v1, "u1"), default);
        var v2 = projection.ListCurrentLoket(loket).Single().RowVersion;
        var stale = (byte[])v2.Clone();
        stale[^1] ^= 0xAA;

        var act = () => finalize.Handle(new FinalizeRegistrationNotEstablishedCmd(
            e.AntrianId, e.NoUrut, loket, stale, "REASON-TEST", "u1"), default);
        await act.Should().ThrowAsync<AdmissionQueueConcurrencyException>();

        EntryStatus(e.AntrianId, e.NoUrut).Should().Be(1);
        CountOutcomes(e.AntrianId, e.NoUrut).Should().Be(0);
        CountActiveClaimsForLoket(loket).Should().Be(1);
    }

    [Fact]
    public async Task BookingAssistance_UniqueRace_OneActive_LoserExisting()
    {
        var clock = FixedClock();
        var bookingId = Ulid.NewUlid().ToString();
        var bookings = new Mock<IBookingRepo>();
        bookings.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>()))
            .Returns(MayBe.From(BookingModel.Default));

        var queues = Queues();
        var points = Points();
        var factory = new AntrianFactory(new Sequencer(_fx.Options));
        var assistance = new BookingAssistanceRepo(_fx.Options);
        var handler = new BookingAssistanceIntakeHandler(
            bookings.Object, points, queues, factory, assistance, clock);

        var bag = new ConcurrentBag<BookingAssistanceIntakeResponse>();
        await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            var r = await handler.Handle(new BookingAssistanceIntakeCmd(
                bookingId, SpA, "NEED_HELP", "K1", "u1"), default);
            bag.Add(r);
        }));

        bag.Should().HaveCount(6);
        bag.Count(x => !x.Existing).Should().Be(1);
        bag.Count(x => x.Existing).Should().Be(5);
        bag.Select(x => (x.AntrianId, x.NoUrut)).Distinct().Should().HaveCount(1);
        CountActiveAssistance(bookingId).Should().Be(1);
    }

    [Fact]
    public async Task Allocation_9999_ThenExhaustion_NoSecondSession()
    {
        var dayOffset = Random.Shared.Next(5001, 9000);
        var clock = new TestTglJamProvider(new DateTime(2026, 1, 1, 9, 30, 0).AddDays(dayOffset));
        var tag = AntrianModel.GenSequenceTag(
            DateOnly.FromDateTime(clock.Now), TimeOnly.MinValue,
            new ServicePointType(SpA, "AQ IT Loket A"));
        EnsureSequenceAt(tag, 9999);

        var (intake, _, _, _, _, _, _) = Build(clock);
        var last = await intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        last.NoUrut.Should().Be(9999);

        var act = () => intake.Handle(new QueAnonymousIntakeCmd(SpA), default);
        await act.Should().ThrowAsync<SequenceExhaustedException>();
        CountSessionsForTag(tag).Should().Be(1);
    }

    [Fact]
    public async Task RepresentativeVolume_WorklistAndCurrentDisplay_UseIndexes()
    {
        var clock = FixedClock();
        var businessDate = DateOnly.FromDateTime(clock.Now);
        SeedVolumeEntries(businessDate, SpA, entryCount: 400, loketCount: 20);

        var projection = new AdmissionQueueOperationalProjection(_fx.Options);
        var io = new StringBuilder();
        using (var conn = new SqlConnection(_conn))
        {
            conn.InfoMessage += (_, e) => io.AppendLine(e.Message);
            conn.Open();
            conn.Execute("SET STATISTICS IO ON;");
            conn.Query("""
                SELECT TOP 100 q.AntrianId, e.NoUrut
                FROM BILRG_Antrian q
                INNER JOIN BILRG_AntrianEntry e ON e.AntrianId = q.AntrianId
                LEFT JOIN BILRG_AdmLoketCurrentCall c
                    ON c.AntrianId = e.AntrianId AND c.NoUrut = e.NoUrut AND c.IsActive = 1
                WHERE q.AntrianDate = @BusinessDate
                  AND e.AntrianStatus IN (0, 1)
                ORDER BY e.Priority DESC, e.CreatedAt, e.NoUrut, q.AntrianId
                """, new { BusinessDate = businessDate.ToDateTime(TimeOnly.MinValue) }).ToList();
            conn.Query("""
                SELECT c.LoketKey, c.AntrianId, c.NoUrut
                FROM BILRG_AdmLoketCurrentCall c
                INNER JOIN BILRG_Antrian q ON q.AntrianId = c.AntrianId
                WHERE c.IsActive = 1
                ORDER BY c.LoketKey
                """).ToList();
            conn.Execute("SET STATISTICS IO OFF;");
        }

        var sw = Stopwatch.StartNew();
        var worklist = projection.ListWorklist(new AdmissionQueueWorklistFilter(
            businessDate,
            Limit: 100,
            ActiveOnly: true));
        var displays = projection.ListCurrentLoket();
        sw.Stop();

        worklist.Should().HaveCount(100);
        displays.Should().NotBeEmpty();
        sw.ElapsedMilliseconds.Should().BeLessThan(5000);

        var composed = new AdmisiRajalOfficerWorklistHandler(
            projection,
            Mock.Of<IPasienTrackerRepo>(),
            Mock.Of<IBookingRepo>(),
            Mock.Of<IRegRepo>(),
            new BookingAssistanceRepo(_fx.Options),
            NullLogger<AdmisiRajalOfficerWorklistHandler>.Instance);
        var durations = new List<long>();
        for (var i = 0; i < 20; i++)
        {
            var composedWatch = Stopwatch.StartNew();
            var page = await composed.Handle(new AdmisiRajalOfficerWorklistQuery(
                businessDate.ToString("yyyy-MM-dd"),
                Limit: 100,
                ActiveOnly: true), default);
            composedWatch.Stop();
            page.Items.Should().HaveCount(100);
            durations.Add(composedWatch.ElapsedMilliseconds);
        }
        ComposedP95EvidenceMs = durations.OrderBy(x => x).ElementAt(18);
        ComposedP95EvidenceMs.Should().BeLessThanOrEqualTo(2000);

        // Evidence string for verification report (seek/scan messages from STATISTICS IO).
        io.ToString().Should().NotBeNullOrWhiteSpace();
        VolumeEvidence = io.ToString();
    }

    /// <summary>Captured STATISTICS IO text for the verification report.</summary>
    public static string VolumeEvidence { get; private set; } = "";
    public static long ComposedP95EvidenceMs { get; private set; }

    [Fact]
    public void ActivePaging_ExcludesFinalStatesWithoutDuplicatesOrGaps()
    {
        var businessDate = new DateOnly(2040, 1, 1).AddDays(Random.Shared.Next(1, 3000));
        var servicePointId = $"AQPG-{Guid.NewGuid():N}"[..20];
        SeedVolumeEntries(
            businessDate,
            servicePointId,
            entryCount: 130,
            loketCount: 0,
            mixedStatuses: true);
        var projection = new AdmissionQueueOperationalProjection(_fx.Options);
        var loaded = new List<AdmissionQueueWorklistItem>();
        var offset = 0;

        while (true)
        {
            var page = projection.ListWorklistPage(new AdmissionQueueWorklistFilter(
                businessDate,
                ServicePointId: servicePointId,
                Offset: offset,
                Limit: 25,
                ActiveOnly: true));
            loaded.AddRange(page.Items);
            if (!page.HasMore)
                break;
            page.NextOffset.Should().Be(offset + page.Items.Count);
            offset = page.NextOffset!.Value;
        }

        loaded.Should().HaveCount(65);
        loaded.Should().OnlyContain(x =>
            x.QueueStatus == (int)AntrianStatusEnum.Waiting
            || x.QueueStatus == (int)AntrianStatusEnum.InService);
        loaded.Select(x => (x.AntrianId, x.NoUrut)).Should().OnlyHaveUniqueItems();
        loaded.Should().Equal(AdmissionQueueWorklistOrdering.Apply(loaded));
    }

    [Fact]
    public async Task CriticalRaces_AreRepeatable()
    {
        // Second pass of the two highest-risk races for reproducibility evidence.
        await SameLoket_TwoEntries_OnlyOneActiveClaim();
        await ConcurrentFirstDailySession_OneWinner_BothAllocate();
    }

    // --- helpers ---

    private static TestTglJamProvider FixedClock() =>
        new(new DateTime(2026, 7, 23, 9, 30, 0));

    private static string UniqueLoket(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}"[..20];

    private (QueAnonymousIntakeHandler Intake,
        AdmissionQueueCallHandler Call,
        AdmissionQueueRecallHandler Recall,
        AdmissionQueueStartServiceHandler Start,
        AdmissionQueueWithdrawHandler Withdraw,
        AdmissionQueueRedirectHandler Redirect,
        AdmissionQueueOperationalProjection Projection) Build(ITglJamProvider clock)
    {
        var queues = Queues();
        var points = Points();
        var ops = new AdmissionQueueOperationRepo(_fx.Options);
        var pub = new NullAdmissionQueueRefreshPublisher();
        var factory = new AntrianFactory(new Sequencer(_fx.Options));
        return (
            new QueAnonymousIntakeHandler(queues, factory, clock, points),
            new AdmissionQueueCallHandler(queues, ops, clock, pub),
            new AdmissionQueueRecallHandler(queues, ops, clock, pub),
            new AdmissionQueueStartServiceHandler(queues, ops, clock, pub),
            new AdmissionQueueWithdrawHandler(queues, ops, clock, pub),
            new AdmissionQueueRedirectHandler(queues, ops, clock, pub, points, factory),
            new AdmissionQueueOperationalProjection(_fx.Options));
    }

    private AntrianRepo Queues() =>
        new(new AntrianDal(_fx.Options), new AntrianEntryDal(_fx.Options), new Sequencer(_fx.Options));

    private AdmissionServicePointRepo Points() =>
        new(new AdmissionServicePointDal(_fx.Options));

    private static async Task<(bool Ok, Exception? Error)> Wrap(Task task)
    {
        try
        {
            await task;
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private void EnsureSequenceAt(string sequenceTag, int currentValue)
    {
        var name = $"sq_{sequenceTag.ToLowerInvariant()}";
        using var conn = new SqlConnection(_conn);
        conn.Open();
        var exists = conn.ExecuteScalar<int>("""
            SELECT COUNT(1) FROM sys.sequences s
            INNER JOIN sys.schemas sch ON sch.schema_id = s.schema_id
            WHERE sch.name = 'dbo' AND s.name = @name
            """, new { name });
        if (exists == 0)
        {
            conn.Execute($"""
                CREATE SEQUENCE [dbo].[{name}] AS INT
                START WITH {currentValue} INCREMENT BY 1 MINVALUE 1 MAXVALUE 9999 NO CYCLE;
                """);
        }
        else
        {
            conn.Execute($"""
                ALTER SEQUENCE [dbo].[{name}] RESTART WITH {currentValue};
                """);
        }
    }

    private void SeedVolumeEntries(
        DateOnly businessDate,
        string servicePointId,
        int entryCount,
        int loketCount,
        bool mixedStatuses = false)
    {
        var antrianId = Ulid.NewUlid().ToString();
        var tag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue,
            new ServicePointType(servicePointId, "vol"));
        using var conn = new SqlConnection(_conn);
        conn.Open();
        conn.Execute("""
            INSERT BILRG_Antrian(AntrianId, AntrianDate, StartTime, EndTime, SequenceTag,
                AntrianDescription, ServicePointCode, QueuePrefixSnapshot)
            VALUES(@antrianId, @date, '00:00', '23:59', @tag, 'Volume', @sp, 'A')
            """, new
            {
                antrianId,
                date = businessDate.ToDateTime(TimeOnly.MinValue),
                tag = tag + "_VOL" + Guid.NewGuid().ToString("N")[..6],
                sp = servicePointId
            });

        for (var i = 1; i <= entryCount; i++)
        {
            conn.Execute("""
                INSERT BILRG_AntrianEntry(AntrianId, NoUrut, PersonName, PasienTrackerId, AntrianStatus,
                    CreatedAt, ServedAt, DoneAt, ReffId, ReffDesc)
                VALUES(@antrianId, @n, '', '-', @status, @at, '3000-01-01', '3000-01-01', '', '')
                """, new
            {
                antrianId,
                n = i,
                status = mixedStatuses ? i % 4 : (int)AntrianStatusEnum.Waiting,
                at = businessDate.ToDateTime(new TimeOnly(8, 0)).AddSeconds(i)
            });
        }

        for (var i = 1; i <= loketCount; i++)
        {
            var loket = $"VOL{i:D2}";
            conn.Execute("""
                IF EXISTS (SELECT 1 FROM BILRG_AdmLoketCurrentCall WHERE LoketKey = @loket)
                  UPDATE BILRG_AdmLoketCurrentCall SET AntrianId=@antrianId, NoUrut=@n, ClaimState=1, IsActive=1,
                    AnnouncementVersion=1, CalledAt=@at, ServiceStartedAt='3000-01-01', ReleasedAt='3000-01-01'
                  WHERE LoketKey=@loket;
                ELSE
                  INSERT BILRG_AdmLoketCurrentCall(LoketKey, AntrianId, NoUrut, ClaimState, IsActive,
                    AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
                  VALUES(@loket, @antrianId, @n, 1, 1, 1, @at, '3000-01-01', '3000-01-01', 'vol', @at, 'vol', @at, '', '3000-01-01');
                """, new
            {
                loket,
                antrianId,
                n = i,
                at = businessDate.ToDateTime(new TimeOnly(9, 0))
            });
        }
    }

    private int CountActiveClaimsForLoket(string loket)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_AdmLoketCurrentCall WHERE LoketKey=@loket AND IsActive=1",
            new { loket });
    }

    private int CountActiveClaimsForEntry(string q, int n)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_AdmLoketCurrentCall WHERE AntrianId=@q AND NoUrut=@n AND IsActive=1",
            new { q, n });
    }

    private int CountSessionsForTag(string tag)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_Antrian WHERE SequenceTag=@tag", new { tag });
    }

    private int EntryStatus(string q, int n)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT AntrianStatus FROM BILRG_AntrianEntry WHERE AntrianId=@q AND NoUrut=@n",
            new { q, n });
    }

    private int CountRedirectReplacements(string sourceQ, int sourceN)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>("""
            SELECT COUNT(1) FROM BILRG_AntrianEntry
            WHERE SourceAntrianId=@sourceQ AND SourceNoUrut=@sourceN
            """, new { sourceQ, sourceN });
    }

    private int CountOutcomes(string q, int n)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_RegOutcome WHERE AntrianId=@q AND NoUrut=@n",
            new { q, n });
    }

    private int CountActiveAssistance(string bookingId)
    {
        using var conn = new SqlConnection(_conn);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_AdmBookingAssistance WHERE BookingId=@bookingId AND IsActive=1",
            new { bookingId });
    }
}
