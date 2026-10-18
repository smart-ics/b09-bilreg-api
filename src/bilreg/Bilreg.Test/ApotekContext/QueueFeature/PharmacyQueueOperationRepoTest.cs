using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.QueueFeature;

[CollectionDefinition(Name, DisableParallelization = true)]
public class ApotekQueueSqlCollection
{
    public const string Name = "ApotekQueueSqlCollection";
}

[Collection(ApotekQueueSqlCollection.Name)]
public class PharmacyQueueOperationRepoTest
{
    private readonly IOptions<DatabaseOptions> _opt;
    private readonly ApotekQueueEntryDal _queueEntryDal;
    private readonly PharmacyServicePointDal _spDal;

    public PharmacyQueueOperationRepoTest()
    {
        _opt = ConnStringHelper.GetTestEnv();
        _queueEntryDal = new ApotekQueueEntryDal(_opt);
        _spDal = new PharmacyServicePointDal(_opt);
    }

    private static string NewId(string prefix = "Q") => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    private PharmacyQueueOperationRepo CreateRepo(ITrackerPharmacyPort? tracker = null)
        => new(_opt, tracker);

    private void SeedQueueEntry(
        string antrianId,
        int noUrut,
        string spId,
        ApotekPhaseEnum phase = ApotekPhaseEnum.TAKEN,
        string prefix = "A")
    {
        var now = DateTime.Now;
        var dto = new ApotekQueueEntryDto(
            AntrianId: antrianId,
            NoUrut: noUrut,
            ServicePointId: spId,
            QueueLabel: $"{prefix}{noUrut:D3}",
            ApotekPhase: (int)phase,
            CallCountPenerimaan: 0,
            LastCalledPenerimaanAt: ApotekDate.Empty,
            CallCountSerah: 0,
            LastCalledSerahAt: ApotekDate.Empty,
            TakenAt: now,
            ServedAt: ApotekDate.Empty,
            PreparedAt: ApotekDate.Empty,
            DoneAt: ApotekDate.Empty,
            WithdrawnAt: ApotekDate.Empty,
            LastSkippedAt: ApotekDate.Empty,
            LastSkippedLoketType: 0,
            WithdrawalReason: "",
            WithdrawalUserId: "",
            IsPaymentOverridden: false,
            PaymentOverrideReason: "",
            PaymentOverridePharmacistId: "",
            CrtUser: "test",
            CrtDate: now,
            UpdUser: "test",
            UpdDate: now,
            VodUser: "",
            VodDate: ApotekDate.Empty);

        _queueEntryDal.Insert(dto);
    }

    private void SeedLoket(
        string loketKey,
        LoketTypeEnum loketType = LoketTypeEnum.PenerimaanResep,
        string depoId = "DEP-01",
        string displayName = "Loket 1")
    {
        const string sql = """
            INSERT INTO BILRG_AptLoketCurrentCall (
                LoketKey, LoketType, DisplayName, DepoFarmasiId,
                AntrianId, NoUrut, ClaimState, IsActive,
                AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            )
            VALUES (
                @loketKey, @loketType, @displayName, @depoId,
                '', 0, 0, 0,
                0, '3000-01-01', '3000-01-01', '3000-01-01',
                'test', GETDATE(), 'test', GETDATE(), '', '3000-01-01'
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(sql, new { loketKey, loketType = (int)loketType, displayName, depoId });
    }

    private void Cleanup(string? antrianId = null, string? loketKey = null, string? spId = null)
    {
        try
        {
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
            if (!string.IsNullOrWhiteSpace(antrianId))
                conn.Execute("DELETE FROM BILRG_AptQueueEntry WHERE AntrianId = @antrianId", new { antrianId });
            if (!string.IsNullOrWhiteSpace(loketKey))
                conn.Execute("DELETE FROM BILRG_AptLoketCurrentCall WHERE LoketKey = @loketKey", new { loketKey });
            if (!string.IsNullOrWhiteSpace(spId))
                conn.Execute("DELETE FROM BILRG_AptServicePoint WHERE ServicePointId = @spId", new { spId });
        }
        catch (SqlException)
        {
            // Transient lock cleanup suppression
        }
    }

    [Fact]
    public void GetLoket_ReturnsEmpty_WhenLoketNotFound()
    {
        var repo = CreateRepo();
        var result = repo.GetLoket("NON_EXISTING_LOKET_" + Guid.NewGuid().ToString("N")[..6]);
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GetLoket_And_ListLoketByDepo_ReturnsPersistedData()
    {
        var loketKey = NewId("LOK");
        var depoId = NewId("DEP");

        try
        {
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep, depoId, "Loket Farmasi Test");
            var repo = CreateRepo();

            var session = repo.GetLoket(loketKey);
            session.HasValue.Should().BeTrue();
            session.Value.LoketKey.Should().Be(loketKey);
            session.Value.LoketType.Should().Be(LoketTypeEnum.PenerimaanResep);
            session.Value.DisplayName.Should().Be("Loket Farmasi Test");
            session.Value.DepoFarmasiId.Should().Be(depoId);
            session.Value.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
            session.Value.IsActive.Should().BeFalse();
            session.Value.RowVersion.Should().NotBeNull();
            session.Value.RowVersion.Length.Should().Be(8);

            var list = repo.ListLoketByDepo(depoId);
            list.Should().ContainSingle(x => x.LoketKey == loketKey);
        }
        finally
        {
            Cleanup(loketKey: loketKey);
        }
    }

    [Fact]
    public void TryCallLoket_SuccessfulCall_IncrementsCounterAndUpdatesSession()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var initialSession = repo.GetLoket(loketKey).Value;
            var success = repo.TryCallLoket(loketKey, antrianId, 1, initialSession.RowVersion, "officer1", now);

            success.Should().BeTrue();

            var updatedSession = repo.GetLoket(loketKey).Value;
            updatedSession.ClaimState.Should().Be(LoketClaimStateEnum.Called);
            updatedSession.IsActive.Should().BeTrue();
            updatedSession.AntrianId.Should().Be(antrianId);
            updatedSession.NoUrut.Should().Be(1);
            updatedSession.AnnouncementVersion.Should().Be(1);
            updatedSession.CalledAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
            updatedSession.RowVersion.Should().NotEqual(initialSession.RowVersion);

            var updatedEntry = _queueEntryDal.Get(antrianId, 1);
            updatedEntry.Should().NotBeNull();
            updatedEntry!.CallCountPenerimaan.Should().Be(1);
            updatedEntry.LastCalledPenerimaanAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryCallLoket_Loket2_IncrementsCallCountSerah()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.SIAP_SERAH);
            SeedLoket(loketKey, LoketTypeEnum.SerahObat);
            var repo = CreateRepo();

            var initialSession = repo.GetLoket(loketKey).Value;
            var success = repo.TryCallLoket(loketKey, antrianId, 1, initialSession.RowVersion, "officer2", now);

            success.Should().BeTrue();

            var updatedEntry = _queueEntryDal.Get(antrianId, 1);
            updatedEntry.Should().NotBeNull();
            updatedEntry!.CallCountSerah.Should().Be(1);
            updatedEntry.CallCountPenerimaan.Should().Be(0);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryCallLoket_Fails_WhenAnotherLoketAlreadyActiveOnSameEntry()
    {
        var antrianId = NewId("ANT");
        var loketKeyA = NewId("LOK_A");
        var loketKeyB = NewId("LOK_B");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKeyA, LoketTypeEnum.PenerimaanResep);
            SeedLoket(loketKeyB, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var sessionA = repo.GetLoket(loketKeyA).Value;
            var sessionB = repo.GetLoket(loketKeyB).Value;

            // Loket A calls ticket
            var callA = repo.TryCallLoket(loketKeyA, antrianId, 1, sessionA.RowVersion, "officerA", now);
            callA.Should().BeTrue();

            // Loket B attempts to call same ticket -> should fail!
            var callB = repo.TryCallLoket(loketKeyB, antrianId, 1, sessionB.RowVersion, "officerB", now);
            callB.Should().BeFalse();
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKeyA, spId: spId);
            Cleanup(loketKey: loketKeyB);
        }
    }

    [Fact]
    public void TryRecallLoket_Success_IncrementsCounterAndAnnouncementVersion()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var recallSuccess = repo.TryRecallLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(10));

            recallSuccess.Should().BeTrue();

            var s3 = repo.GetLoket(loketKey).Value;
            s3.AnnouncementVersion.Should().Be(2);

            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.CallCountPenerimaan.Should().Be(2);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryRecallLoket_Fails_OnStaleRowVersion()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var staleToken = (byte[])s2.RowVersion.Clone();

            // Perform valid recall which mutates RowVersion
            repo.TryRecallLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(5)).Should().BeTrue();

            // Attempt recall with stale token -> should fail!
            var staleRecall = repo.TryRecallLoket(loketKey, staleToken, "officer1", now.AddSeconds(10));
            staleRecall.Should().BeFalse();
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryStartServiceLoket_TransitionsToInService_AndSynchronizesTracker()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        var mockTracker = new Mock<ITrackerPharmacyPort>();
        mockTracker.Setup(t => t.ServeOnce(antrianId, 1, It.IsAny<string>(), loketKey, It.IsAny<DateTime>()))
            .Returns(new TrackerPharmacyCommandResult(true, "served"));

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo(mockTracker.Object);

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var startSuccess = repo.TryStartServiceLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(20));

            startSuccess.Should().BeTrue();

            var s3 = repo.GetLoket(loketKey).Value;
            s3.ClaimState.Should().Be(LoketClaimStateEnum.InService);
            s3.ServiceStartedAt.Should().BeCloseTo(now.AddSeconds(20), TimeSpan.FromSeconds(2));

            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.ApotekPhase.Should().Be((int)ApotekPhaseEnum.PENERIMAAN);
            entry.ServedAt.Should().BeCloseTo(now.AddSeconds(20), TimeSpan.FromSeconds(2));

            mockTracker.Verify(t => t.ServeOnce(antrianId, 1, "-", loketKey, It.IsAny<DateTime>()), Times.Once);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryStartServiceLoket_RollsBack_WhenTrackerThrowsConcurrencyException()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        var mockTracker = new Mock<ITrackerPharmacyPort>();
        mockTracker.Setup(t => t.ServeOnce(antrianId, 1, It.IsAny<string>(), loketKey, It.IsAny<DateTime>()))
            .Throws(new ApotekConcurrencyException($"{antrianId}:1", 1));

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo(mockTracker.Object);

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var startSuccess = repo.TryStartServiceLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(20));

            startSuccess.Should().BeFalse();

            // Sesi loket harus tetap Called (rollback atomic)
            var s3 = repo.GetLoket(loketKey).Value;
            s3.ClaimState.Should().Be(LoketClaimStateEnum.Called);

            // Antrian entry harus tetap TAKEN (rollback atomic)
            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.ApotekPhase.Should().Be((int)ApotekPhaseEnum.TAKEN);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryReleaseLoket_Success_ReleasesClaimToIdle()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var releaseSuccess = repo.TryReleaseLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(30));

            releaseSuccess.Should().BeTrue();

            var s3 = repo.GetLoket(loketKey).Value;
            s3.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
            s3.IsActive.Should().BeFalse();
            s3.ReleasedAt.Should().BeCloseTo(now.AddSeconds(30), TimeSpan.FromSeconds(2));
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TryReleaseLoket_Fails_OnStaleRowVersion()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var staleToken = (byte[])s2.RowVersion.Clone();

            // Modify loket (recall)
            repo.TryRecallLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(5));

            // Release with stale token -> should fail!
            var releaseFail = repo.TryReleaseLoket(loketKey, staleToken, "officer1", now.AddSeconds(30));
            releaseFail.Should().BeFalse();

            var s3 = repo.GetLoket(loketKey).Value;
            s3.ClaimState.Should().Be(LoketClaimStateEnum.Called);
            s3.IsActive.Should().BeTrue();
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TrySkipLoket_Success_TransitionsQueueEntryToDilewati_AndReleasesLoket()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var skipSuccess = repo.TrySkipLoket(
                loketKey,
                "Pasien tidak menyahut 3x pemanggilan",
                s2.RowVersion,
                "officer1",
                now.AddSeconds(45));

            skipSuccess.Should().BeTrue();

            var s3 = repo.GetLoket(loketKey).Value;
            s3.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
            s3.IsActive.Should().BeFalse();

            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.ApotekPhase.Should().Be((int)ApotekPhaseEnum.DILEWATI);
            entry.LastSkippedAt.Should().BeCloseTo(now.AddSeconds(45), TimeSpan.FromSeconds(2));
            entry.LastSkippedLoketType.Should().Be((int)LoketTypeEnum.PenerimaanResep);
            entry.WithdrawalReason.Should().Be("Pasien tidak menyahut 3x pemanggilan");
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public void TrySkipLoket_Fails_OnStaleRowVersion()
    {
        var antrianId = NewId("ANT");
        var loketKey = NewId("LOK");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var s1 = repo.GetLoket(loketKey).Value;
            repo.TryCallLoket(loketKey, antrianId, 1, s1.RowVersion, "officer1", now);

            var s2 = repo.GetLoket(loketKey).Value;
            var staleToken = (byte[])s2.RowVersion.Clone();

            // Recall bumps RowVersion
            repo.TryRecallLoket(loketKey, s2.RowVersion, "officer1", now.AddSeconds(5));

            var skipResult = repo.TrySkipLoket(loketKey, "Reason", staleToken, "officer1", now.AddSeconds(15));
            skipResult.Should().BeFalse();

            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.ApotekPhase.Should().Be((int)ApotekPhaseEnum.TAKEN);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKey, spId: spId);
        }
    }

    [Fact]
    public async Task RaceCondition_TwoOfficersCallingSameTicketSimultaneously_OnlyOneSucceeds()
    {
        // Pengujian balapan konkurensi (simulasi 2 petugas memanggil nomor yang sama secara bersamaan)
        // Menghasilkan 1 sukses dan 1 konflik (HTTP 409 / Concurrency Exception).
        var antrianId = NewId("ANT");
        var loketKeyA = NewId("LOK_A");
        var loketKeyB = NewId("LOK_B");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKeyA, LoketTypeEnum.PenerimaanResep);
            SeedLoket(loketKeyB, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var sessionA = repo.GetLoket(loketKeyA).Value;
            var sessionB = repo.GetLoket(loketKeyB).Value;

            // Helper simulating high-level command handler behavior throwing Concurrency Exception on failure
            async Task<(bool Ok, Exception? Error)> SimulateCall(string loket, byte[] token, string user)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var ok = repo.TryCallLoket(loket, antrianId, 1, token, user, now);
                        if (!ok)
                            throw new ApotekConcurrencyException($"{antrianId}:1", 1);
                        return (true, (Exception?)null);
                    }
                    catch (Exception ex)
                    {
                        return (false, ex);
                    }
                });
            }

            var results = await Task.WhenAll(
                SimulateCall(loketKeyA, sessionA.RowVersion, "petugas_1"),
                SimulateCall(loketKeyB, sessionB.RowVersion, "petugas_2"));

            results.Count(r => r.Ok).Should().Be(1, "Tepat 1 petugas harus berhasil mengklaim tiket antrean");
            results.Count(r => !r.Ok).Should().Be(1, "Petugas kedua yang kalah balapan harus mengalami konflik");
            results.Single(r => !r.Ok).Error.Should().BeOfType<ApotekConcurrencyException>(
                "Kekalahan balapan konkurensi menghasilkan Concurrency Exception (HTTP 409)");

            // Verifikasi di database: hanya 1 loket yang aktif memegang tiket tersebut
            var sA = repo.GetLoket(loketKeyA).Value;
            var sB = repo.GetLoket(loketKeyB).Value;

            var activeClaims = new[] { sA, sB }.Count(s => s.IsActive && s.AntrianId == antrianId && s.NoUrut == 1);
            activeClaims.Should().Be(1);

            var entry = _queueEntryDal.Get(antrianId, 1);
            entry!.CallCountPenerimaan.Should().Be(1);
        }
        finally
        {
            Cleanup(antrianId: antrianId, loketKey: loketKeyA, spId: spId);
            Cleanup(loketKey: loketKeyB);
        }
    }

    [Fact]
    public async Task RaceCondition_TwoOfficersCallingDifferentTicketsOnSameLoket_OnlyOneSucceeds()
    {
        var antrianId1 = NewId("ANT1");
        var antrianId2 = NewId("ANT2");
        var loketKey = NewId("LOK_SAME");
        var spId = NewId("SP");
        var now = DateTime.Now;

        try
        {
            SeedQueueEntry(antrianId1, 1, spId, ApotekPhaseEnum.TAKEN);
            SeedQueueEntry(antrianId2, 2, spId, ApotekPhaseEnum.TAKEN);
            SeedLoket(loketKey, LoketTypeEnum.PenerimaanResep);
            var repo = CreateRepo();

            var session = repo.GetLoket(loketKey).Value;

            async Task<(bool Ok, Exception? Error)> SimulateCall(string antrianId, int noUrut, string user)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        var ok = repo.TryCallLoket(loketKey, antrianId, noUrut, session.RowVersion, user, now);
                        if (!ok)
                            throw new ApotekConcurrencyException(loketKey, 1);
                        return (true, (Exception?)null);
                    }
                    catch (Exception ex)
                    {
                        return (false, ex);
                    }
                });
            }

            var results = await Task.WhenAll(
                SimulateCall(antrianId1, 1, "officer_1"),
                SimulateCall(antrianId2, 2, "officer_2"));

            results.Count(r => r.Ok).Should().Be(1);
            results.Count(r => !r.Ok).Should().Be(1);
            results.Single(r => !r.Ok).Error.Should().BeOfType<ApotekConcurrencyException>();

            var finalLoket = repo.GetLoket(loketKey).Value;
            finalLoket.ClaimState.Should().Be(LoketClaimStateEnum.Called);
            finalLoket.IsActive.Should().BeTrue();
        }
        finally
        {
            Cleanup(antrianId: antrianId1, loketKey: loketKey, spId: spId);
            Cleanup(antrianId: antrianId2);
        }
    }
}
