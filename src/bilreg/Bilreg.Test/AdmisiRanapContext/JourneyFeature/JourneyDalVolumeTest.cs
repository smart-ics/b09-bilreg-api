using System.Reflection;
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
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Representative-volume journey list performance and pagination integrity.
/// Same env/schema requirements as JourneyDalIntegration; always rolls back.
/// </summary>
[Collection("JourneyDalDb")]
[Trait("Category", "JourneyDalVolume")]
public class JourneyDalVolumeTest
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);
    private readonly JourneyDal _sut;
    private readonly IOptions<DatabaseOptions> _opt;

    public JourneyDalVolumeTest()
    {
        ResetConnStringCache();
        var (opt, connStr) = JourneyDalTestEnv.RequireConfiguredConnection();
        JourneyDalTestEnv.RequireSchema(connStr);
        _opt = opt;
        _sut = new JourneyDal(_opt);
    }

    [Fact]
    public void Volume_500_Journeys_Pagination_No_Duplication_Or_Omission()
    {
        using var trans = TransHelper.NewScope();
        var pasienId = "V" + Guid.NewGuid().ToString("N")[..8];
        var expectedIds = SeedCorpus(pasienId, journeyCount: 500);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = _sut.List(new JourneyListFilter(
                JourneyListScope.History,
                SearchTerm: pasienId,
                PageSize: 50,
                Cursor: cursor));
            pages++;
            foreach (var item in page.Items)
                seen.Add(item.JourneyId).Should().BeTrue($"duplicate JourneyId {item.JourneyId}");
            cursor = page.NextCursor;
        } while (cursor is not null && pages < 30);

        expectedIds.Except(seen).Should().BeEmpty("all seeded journeys must appear across cursor pages");
        seen.Count.Should().BeGreaterThanOrEqualTo(expectedIds.Count);
    }

    [Fact]
    public void Volume_PageSizes_And_Filters_Report_Fixed_RoundTrips_And_Timings()
    {
        using var trans = TransHelper.NewScope();
        var pasienId = "V" + Guid.NewGuid().ToString("N")[..8];
        SeedCorpus(pasienId, journeyCount: 500);

        JourneyListDiagnostics? page10 = null;
        JourneyListDiagnostics? page50 = null;
        JourneyListDiagnostics? filtered = null;
        JourneyListDiagnostics? history = null;

        _ = _sut.List(new JourneyListFilter(JourneyListScope.Active, SearchTerm: pasienId, PageSize: 10));
        page10 = _sut.LastListDiagnostics;

        _ = _sut.List(new JourneyListFilter(JourneyListScope.Active, SearchTerm: pasienId, PageSize: 50));
        page50 = _sut.LastListDiagnostics;

        _ = _sut.List(new JourneyListFilter(
            JourneyListScope.Active,
            Stage: JourneyOperationalStage.HandoverRequired,
            SearchTerm: pasienId,
            PageSize: 50));
        filtered = _sut.LastListDiagnostics;

        _ = _sut.List(new JourneyListFilter(JourneyListScope.History, SearchTerm: pasienId, PageSize: 50));
        history = _sut.LastListDiagnostics;

        foreach (var diag in new[] { page10, page50, filtered, history })
        {
            diag.Should().NotBeNull();
            diag!.DatabaseRoundTrips.Should().Be(JourneyDal.FixedListRoundTripsWithPage);
        }

        page50!.DatabaseRoundTrips.Should().Be(page10!.DatabaseRoundTrips);

        // Soft performance signal for the report — do not fail the suite solely on remote latency.
        Console.WriteLine(
            $"JourneyDalVolume timings (ms): page10={page10.TotalListMs} page50={page50.TotalListMs} " +
            $"filtered={filtered!.TotalListMs} history={history!.TotalListMs} " +
            $"batch50={page50.BatchHydrationMs} resolve50={page50.ResolverEnrichmentMs} " +
            $"facetPage50={page50.FacetPageSqlMs} rt={page50.DatabaseRoundTrips}");

        var detailReg = NextId("R", 10);
        SeedAdmission(detailReg, pasienId, "-", "-");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var detail = _sut.GetByJourneyId($"reg:{detailReg}");
        sw.Stop();
        detail.Should().NotBeNull();
        Console.WriteLine($"JourneyDalVolume detail ms={sw.ElapsedMilliseconds} journeyId=reg:{detailReg}");
    }

    [Fact]
    public void Volume_Cursor_Continuation_With_Equal_Timestamps_Is_Stable()
    {
        using var trans = TransHelper.NewScope();
        var pasienId = "V" + Guid.NewGuid().ToString("N")[..8];
        var stamp = new DateTime(2026, 7, 13, 15, 0, 0);
        var ids = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var regId = NextId("R", 10);
            SeedAdmission(regId, pasienId, "-", "-", admissionDate: stamp);
            ids.Add($"reg:{regId}");
        }

        var all = new List<string>();
        string? cursor = null;
        for (var p = 0; p < 5; p++)
        {
            var page = _sut.List(new JourneyListFilter(SearchTerm: pasienId, PageSize: 10, Cursor: cursor));
            all.AddRange(page.Items.Select(i => i.JourneyId));
            cursor = page.NextCursor;
            if (cursor is null)
                break;
        }

        all.Should().OnlyHaveUniqueItems();
        ids.Except(all).Should().BeEmpty();
    }

    private HashSet<string> SeedCorpus(string pasienId, int journeyCount)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        // Mix: prospective opn, prospective rsv, admissions (some multi-WL), cancelled, recon
        var perBucket = journeyCount / 6;
        for (var i = 0; i < perBucket; i++)
        {
            var opn = NextId("O", 12);
            SeedOpname(opn, pasienId, (int)OpnameRequestStatusEnum.Requested);
            ids.Add($"opn:{opn}");
        }

        for (var i = 0; i < perBucket; i++)
        {
            var rsv = NextId("S", 12);
            SeedReservation(rsv, pasienId, (int)ReservationStatusEnum.Reserved);
            ids.Add($"rsv:{rsv}");
        }

        for (var i = 0; i < perBucket; i++)
        {
            var reg = NextId("R", 10);
            SeedAdmission(reg, pasienId, "-", "-");
            ids.Add($"reg:{reg}");
            if (i % 5 == 0)
            {
                SeedWaitingList(NextId("W", 12), reg, pasienId, (int)WaitingListStatusEnum.Waiting,
                    at: DateTime.Now.AddHours(-2));
                SeedWaitingList(NextId("W", 12), reg, pasienId, (int)WaitingListStatusEnum.Closed,
                    at: DateTime.Now.AddHours(-1));
            }
        }

        for (var i = 0; i < perBucket; i++)
        {
            var opn = NextId("O", 12);
            var reg = NextId("R", 10);
            SeedOpname(opn, pasienId, (int)OpnameRequestStatusEnum.Fulfilled, reg);
            SeedAdmission(reg, pasienId, opn, "-");
            ids.Add($"opn:{opn}");
        }

        for (var i = 0; i < perBucket; i++)
        {
            var opn = NextId("O", 12);
            SeedOpname(opn, pasienId, (int)OpnameRequestStatusEnum.Cancelled);
            ids.Add($"opn:{opn}");
        }

        // Remainder: accepted WL + both-sources recon
        while (ids.Count < journeyCount)
        {
            var reg = NextId("R", 10);
            SeedAdmission(reg, pasienId, "-", "-");
            SeedWaitingList(NextId("W", 12), reg, pasienId, (int)WaitingListStatusEnum.Accepted);
            ids.Add($"reg:{reg}");
        }

        // One reconciliation case
        var reconReg = NextId("R", 10);
        var reconOpn = NextId("O", 12);
        var reconRsv = NextId("S", 12);
        SeedOpname(reconOpn, pasienId, (int)OpnameRequestStatusEnum.Fulfilled, reconReg);
        SeedReservation(reconRsv, pasienId, (int)ReservationStatusEnum.Realized, reconReg);
        SeedAdmission(reconReg, pasienId, reconOpn, reconRsv);
        ids.Add($"reg:{reconReg}");

        return ids;
    }

    private void SeedOpname(string id, string pasienId, int status, string fulfilledRegId = "-")
    {
        new OpnameRequestDal(_opt).Insert(new OpnameRequestDto(
            id, status, pasienId, "", "3000-01-01", "",
            "D001", "Dokter Uji", DateTime.Now, "notes", fulfilledRegId,
            "journey-vol", DateTime.Now, "journey-vol", DateTime.Now, "", VoidSentinel));
    }

    private void SeedReservation(string id, string pasienId, int status, string realizedRegId = "-")
    {
        new ReservationDal(_opt).Insert(new ReservationDto(
            id, status, pasienId, "Pasien Uji", new DateTime(1990, 1, 1), "L",
            DateTime.Now, "1", "Kelas 1", "B001", "Bangsal A", realizedRegId,
            "journey-vol", DateTime.Now, "journey-vol", DateTime.Now, "", VoidSentinel));
    }

    private void SeedAdmission(
        string regId,
        string pasienId,
        string opnameRequestId,
        string reservationId,
        DateTime? admissionDate = null)
    {
        new AdmissionDal(_opt).Insert(new AdmissionDto(
            regId,
            (int)AdmissionStatusEnum.Admitted,
            0,
            pasienId, "", "3000-01-01", "",
            opnameRequestId, reservationId,
            "1", "Kelas 1", "B001", "Bangsal A",
            admissionDate ?? DateTime.Now,
            "journey-vol", admissionDate ?? DateTime.Now,
            "journey-vol", admissionDate ?? DateTime.Now,
            "", VoidSentinel));
    }

    private void SeedWaitingList(string wlId, string regId, string pasienId, int status, DateTime? at = null)
    {
        var stamp = at ?? DateTime.Now;
        new WaitingListDal(_opt).Insert(new WaitingListDto(
            wlId, status, regId, pasienId, "", "3000-01-01", "",
            "1", "Kelas 1", "B001", "Bangsal A", 5,
            "journey-vol", stamp, "journey-vol", stamp, "", VoidSentinel));
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
}
