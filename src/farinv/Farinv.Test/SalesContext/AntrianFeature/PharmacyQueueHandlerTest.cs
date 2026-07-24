using Bilreg.Domain.AdmisiContext.RegFeature;
using Farinv.Application.SalesContext.AntrianFeature;
using Farinv.Application.SalesContext.AntrianFeature.UsesCases;
using Farinv.Domain.SalesContext.AntrianFeature;
using FluentAssertions;
using Nuna.Lib.PatternHelper;

namespace Farinv.Test.SalesContext.AntrianFeature;

public class PharmacyQueueHandlerTest
{
    private static readonly RegReff SampleReg = new("RG00000001", "PS001", "Sinta");
    private const string TrackerId = "01HXYZABCDEFGHJKMNPQRSTVWXY";

    [Fact]
    public void ConfirmSale_AppendsApotekStartAndSetsServedAt()
    {
        var repo = new FakeAntrianRepo();
        var appendCalls = new List<AppendPharmacyEvidenceRequest>();
        var appendService = new FakeAppendTrackerEvidenceService(appendCalls);
        var antrian = AntrianModel.Create(DateOnly.FromDateTime(DateTime.Today), 1, "Apotek RJ");
        antrian.AddEntryByTracker(1, SampleReg, TrackerId, new DateTime(2025, 8, 3, 7, 40, 0));
        repo.Seed(antrian);

        var servedAt = new DateTime(2025, 8, 3, 7, 51, 0);
        var handler = new ConfirmPharmacySaleHandler(repo, appendService);
        handler.Handle(new QueConfirmPharmacySaleCmd(TrackerId, 1, "DU-071", servedAt), CancellationToken.None)
            .GetAwaiter().GetResult();

        var saved = repo.LoadOrCreate(1);
        var entry = saved.GetActiveEntryByTracker(TrackerId);
        entry.ServedAt.Should().Be(servedAt);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Prepared);
        appendCalls.Should().ContainSingle(x =>
            x.PasienTrackerId == TrackerId
            && x.EventName == ConfirmPharmacySaleHandler.ApotekStartEventName
            && x.ReffId == "DU-071"
            && x.OccurredAt == servedAt);
    }

    [Fact]
    public void Deliver_AppendsApotekDoneAndMarksDelivered()
    {
        var repo = new FakeAntrianRepo();
        var appendCalls = new List<AppendPharmacyEvidenceRequest>();
        var appendService = new FakeAppendTrackerEvidenceService(appendCalls);
        var antrian = AntrianModel.Create(DateOnly.FromDateTime(DateTime.Today), 1, "Apotek RJ");
        antrian.AddEntryByTracker(1, SampleReg, TrackerId, new DateTime(2025, 8, 3, 7, 40, 0));
        antrian.ConfirmPharmacySale(1, "DU-071", new DateTime(2025, 8, 3, 7, 51, 0));
        repo.Seed(antrian);

        var doneAt = new DateTime(2025, 8, 3, 8, 5, 0);
        var handler = new DeliverAntrianHandler(repo, appendService);
        handler.Handle(new QueDeliverAntrianCmd(TrackerId, 1, doneAt), CancellationToken.None)
            .GetAwaiter().GetResult();

        var saved = repo.LoadOrCreate(1);
        var entry = saved.ListEntry.Single();
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Delivered);
        appendCalls.Should().ContainSingle(x =>
            x.PasienTrackerId == TrackerId
            && x.EventName == DeliverAntrianHandler.ApotekDoneEventName
            && x.ReffId == PharmacyQueueEvidenceReference.Create(antrian.AntrianId, 1)
            && x.OccurredAt == doneAt);
    }

    private sealed class FakeAntrianRepo : IAntrianRepo
    {
        private AntrianModel? _model;

        public void Seed(AntrianModel model) => _model = model;

        public AntrianModel LoadOrCreate(int servicePoint)
            => _model ?? AntrianModel.Create(DateOnly.FromDateTime(DateTime.Today), servicePoint, $"Apotek {servicePoint}");

        public void SaveChanges(AntrianModel model) => _model = model;

        public MayBe<AntrianModel> LoadEntity(IAntrianKey key)
            => MayBe.From(_model!);

        public void DeleteEntity(IAntrianKey key) => _model = null;

        public IEnumerable<AntrianHeaderView> ListData(DateOnly filter) => [];

        public IEnumerable<AntrianView> ListData(DateTime dateTime) => [];
    }

    private sealed class FakeAppendTrackerEvidenceService(List<AppendPharmacyEvidenceRequest> calls)
        : IAppendTrackerEvidenceService
    {
        public AppendPharmacyEvidenceRequest Execute(AppendPharmacyEvidenceRequest request)
        {
            calls.Add(request);
            return request;
        }
    }
}
