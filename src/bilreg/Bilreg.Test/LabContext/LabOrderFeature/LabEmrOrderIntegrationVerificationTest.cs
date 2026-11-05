using System.Net;
using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

/// <summary>
/// Pengujian Verifikasi Terpadu Alur EMR -> Bilreg LWF -> OWARE -> Worklist Lab (Slice P4-S06).
/// Memvalidasi 4 skenario terpadu:
/// 1. Happy Path Order Baru EMR, Idempotensi, & Worklist Laboratorium
/// 2. Peringatan Duplikasi 24 Jam & Konfirmasi Override DPJP (termasuk respons 409 JSend)
/// 3. Siklus Hidup & Proteksi Pembatalan Berdasarkan Status Pengambilan Spesimen
/// 4. Penerusan Otomatis ke LIS / OWARE Outbound Queue saat Status Charged
/// </summary>
public class LabEmrOrderIntegrationVerificationTest
{
    private static readonly DateTime FixedNow = new(2026, 11, 2, 9, 30, 0);

    // In-memory repositories state
    private readonly Dictionary<string, LabOrderModel> _orderStore = new();
    private readonly Dictionary<string, LabOrderModel> _orderStoreByEmrId = new();
    private readonly Dictionary<string, LabOwareOutboundQueueModel> _queueStore = new();

    // Dependencies & Mocks
    private readonly Mock<ILabOrderRepo> _labOrderRepoMock = new();
    private readonly Mock<ISequencer> _sequencerMock = new();
    private readonly Mock<ILabTestResolutionService> _resolutionMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<ILabOrderDal> _labOrderDalMock = new();
    private readonly Mock<ILabOrderWorklistDal> _worklistDalMock = new();
    private readonly Mock<ILabBillingIntegration> _billingIntegrationMock = new();
    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepoMock = new();
    private readonly Mock<ILabOwareIntegration> _owareIntegrationMock = new();
    private readonly TestTglJamProvider _tglJamProvider = new(FixedNow);

    // Handlers under test
    private readonly LabOrderCreateFromEmrHandler _createFromEmrHandler;
    private readonly LabOrderGetByEmrOrderIdHandler _getByEmrOrderIdHandler;
    private readonly LabOrderWorklistHandler _worklistHandler;
    private readonly LabOrderChargeHandler _chargeHandler;
    private readonly LabOrderCancelHandler _cancelHandler;
    private readonly LabOwareQueueProcessor _owareQueueProcessor;

    private int _orderNoSequence = 200;

    public LabEmrOrderIntegrationVerificationTest()
    {
        // Setup in-memory ILabOrderRepo
        _labOrderRepoMock.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m =>
            {
                _orderStore[m.OrderId] = m;
                if (!string.IsNullOrWhiteSpace(m.EmrOrderId))
                {
                    _orderStoreByEmrId[m.EmrOrderId] = m;
                }
            });

        _labOrderRepoMock.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns((ILabOrderKey k) =>
                _orderStore.TryGetValue(k.OrderId, out var m) ? MayBe.From(m) : MayBe<LabOrderModel>.None);

        _labOrderRepoMock.Setup(r => r.LoadByEmrOrderId(It.IsAny<string>()))
            .Returns((string emrId) =>
                _orderStoreByEmrId.TryGetValue(emrId, out var m) ? MayBe.From(m) : MayBe<LabOrderModel>.None);

        // Setup in-memory ILabOwareOutboundQueueRepo
        _queueRepoMock.Setup(q => q.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()))
            .Callback<LabOwareOutboundQueueModel>(q => _queueStore[q.QueueId] = q);

        _queueRepoMock.Setup(q => q.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns((ILabOwareOutboundQueueKey k) =>
                _queueStore.TryGetValue(k.QueueId, out var q) ? MayBe.From(q) : MayBe<LabOwareOutboundQueueModel>.None);

        _queueRepoMock.Setup(q => q.ListProcessable(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int batchSize, int maxRetry) =>
                _queueStore.Values
                    .Where(q => q.QueueStatus == LabOwareQueueStatusEnum.Pending && q.RetryCount <= maxRetry)
                    .Take(batchSize)
                    .ToList());

        // Setup Sequencer
        _sequencerMock.Setup(s => s.GetNextNoUrut(It.IsAny<string>()))
            .Returns(() => ++_orderNoSequence);

        // Setup Tarif Resolution
        _resolutionMock.Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns((IEnumerable<LabOrderTarifItemInput> items) =>
                items.Select(i => new ResolvedLabOrderLine(
                    LabOrderItemModel.Create(
                        "LTD-" + i.TarifId,
                        i.TarifId,
                        i.TarifName ?? i.TarifId,
                        i.TarifId,
                        "CODE-" + i.TarifId,
                        i.TarifName ?? i.TarifId,
                        VacutainerTypeEnum.Edta,
                        "Whole Blood",
                        1),
                    [LabOrderTestSupport.SampleComponent()]
                )).ToList());

        // Setup Active Reg Repo
        _regRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns((IRegKey k) => MayBe.From(CreateActiveReg(k.RegId)));

        // Setup Doctor PPA Repo
        _ppaRepoMock.Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns((IPpaKey k) => MayBe.From(PpaType.Default with
            {
                PpaId = k.PpaId,
                PpaName = "dr. John Doe, Sp.PK"
            }));

        // Setup Default Active Tests (no duplicates by default)
        _labOrderDalMock.Setup(d => d.ListActiveTestsByPatientWithinHours(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns(new List<LabOrderActiveTestDto>());

        // Setup Worklist DAL reading from in-memory order store
        _worklistDalMock.Setup(d => d.List(It.IsAny<LabOrderWorklistFilter>()))
            .Returns((LabOrderWorklistFilter filter) =>
            {
                return _orderStore.Values
                    .Where(o => !filter.LabOrderStatus.HasValue || (int)o.LabOrderStatus == filter.LabOrderStatus.Value)
                    .Select(o => new LabOrderWorklistView(
                        o.OrderId,
                        o.OrderNo,
                        (int)o.LabOrderStatus,
                        (int)o.OrderSource,
                        o.Patient.PatientId,
                        o.Patient.PatientName,
                        o.Patient.Gender,
                        o.Patient.AgeAtOrder,
                        o.Items.Count,
                        0,
                        0,
                        o.AuditTrail.Created.Timestamp,
                        "",
                        IsCito: o.IsCito,
                        DokterPerujukName: o.DokterPerujukName,
                        LayananPerujukName: o.LayananPerujukName,
                        DeferredReason: o.DeferredInfo.Reason,
                        TestNames: o.Items.Select(x => x.LabTestName).ToList()
                    )).ToList();
            });

        // Setup Billing Integration
        _billingIntegrationMock.Setup(b => b.ChargeOrder(It.IsAny<LabOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns((LabOrderModel o, string user, DateTime at) => "TDK-" + o.OrderNo);

        // Setup OWARE Integration
        _owareIntegrationMock.Setup(o => o.Send(It.IsAny<string>()))
            .Returns(new LabOwareSendResult(true, null));

        // Instantiate handlers
        _createFromEmrHandler = new LabOrderCreateFromEmrHandler(
            _labOrderRepoMock.Object,
            _sequencerMock.Object,
            _resolutionMock.Object,
            _tglJamProvider,
            _regRepoMock.Object,
            _ppaRepoMock.Object,
            _labOrderDalMock.Object);

        _getByEmrOrderIdHandler = new LabOrderGetByEmrOrderIdHandler(_labOrderRepoMock.Object);

        _worklistHandler = new LabOrderWorklistHandler(_worklistDalMock.Object);

        _chargeHandler = new LabOrderChargeHandler(
            _labOrderRepoMock.Object,
            _billingIntegrationMock.Object,
            _tglJamProvider,
            _queueRepoMock.Object);

        _cancelHandler = new LabOrderCancelHandler(
            _labOrderRepoMock.Object,
            _billingIntegrationMock.Object,
            _tglJamProvider,
            _queueRepoMock.Object);

        _owareQueueProcessor = new LabOwareQueueProcessor(
            _queueRepoMock.Object,
            _labOrderRepoMock.Object,
            _owareIntegrationMock.Object);
    }

    private static RegModel CreateActiveReg(string regId = "REG001") =>
        new(
            regId,
            new DateOnly(2026, 11, 1),
            new AuditInfoType("tester", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

    private static LabOrderCreateFromEmrCmd CreateSampleEmrCmd(
        string emrOrderId = "EMR-CHART-001",
        string tarifId = "TRF-CBC",
        string tarifName = "Darah Lengkap",
        bool allowDuplicate = false) =>
        new(
            UserId: "DOKTER01",
            EmrOrderId: emrOrderId,
            RegId: "REG001",
            PatientId: "RM-9901",
            PatientName: "Tn. Ahmad Sobari",
            BirthDateYmd: "1980-08-15",
            Gender: "L",
            Items: [new LabOrderTarifItemInput(tarifId, tarifName)],
            DokterPerujukId: "DR01",
            DokterPerujukName: "dr. John Doe, Sp.PK",
            LayananPerujukId: "POLI-INT",
            LayananPerujukName: "Poli Penyakit Dalam",
            ClinicalNote: "Evaluasi anemia",
            Urgency: 0,
            AllowDuplicateConfirmation: allowDuplicate);

    #region Skenario 1: Happy Path Order Baru EMR, Idempotensi, & Worklist Laboratorium

    [Fact]
    public async Task Scenario1_CreateOrderFromEmr_GeneratesOrderNo_StoresEmrOrderId_AndPersistsOrderedStatus()
    {
        // 1. Kirim order lab dari EMR
        var cmd = CreateSampleEmrCmd("EMR-CHART-101");
        var response = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);

        // Assert response use case
        response.Should().NotBeNull();
        response.EmrOrderId.Should().Be("EMR-CHART-101");
        response.OrderNo.Should().NotBeNullOrWhiteSpace();
        response.OrderNo.Should().StartWith("LAB");
        response.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        response.OrderId.Should().NotBeNullOrWhiteSpace();

        // 2. Verifikasi order tersimpan pada repository dengan status Ordered
        _orderStore.Should().ContainKey(response.OrderId);
        var storedOrder = _orderStore[response.OrderId];
        storedOrder.EmrOrderId.Should().Be("EMR-CHART-101");
        storedOrder.OrderNo.Should().Be(response.OrderNo);
        storedOrder.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        storedOrder.Patient.PatientId.Should().Be("RM-9901");
        storedOrder.Patient.PatientName.Should().Be("Tn. Ahmad Sobari");

        // 3. Verifikasi kueri status real-time EMR (LabOrderGetByEmrOrderIdHandler)
        var emrStatus = await _getByEmrOrderIdHandler.Handle(
            new LabOrderGetByEmrOrderIdQuery("EMR-CHART-101"), CancellationToken.None);

        emrStatus.Should().NotBeNull();
        emrStatus.EmrOrderId.Should().Be("EMR-CHART-101");
        emrStatus.OrderNo.Should().Be(response.OrderNo);
        emrStatus.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
    }

    [Fact]
    public async Task Scenario1_Idempotency_RepeatEmrOrder_ReturnsExistingOrderWithoutDuplication()
    {
        // 1. Order pertama kali dibuat
        var cmd = CreateSampleEmrCmd("EMR-CHART-IDEMP-01");
        var firstResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);

        _orderStore.Count.Should().Be(1);
        var initialOrderCount = _orderStore.Count;

        // 2. Pemanggilan ulang dengan EmrOrderId yang sama (simulasi retry EMR atau timeout)
        var secondResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);

        // Assert: mengembalikan order yang sama persis tanpa menambah record
        secondResponse.Should().NotBeNull();
        secondResponse.OrderId.Should().Be(firstResponse.OrderId);
        secondResponse.OrderNo.Should().Be(firstResponse.OrderNo);
        secondResponse.EmrOrderId.Should().Be(firstResponse.EmrOrderId);
        secondResponse.LabOrderStatus.Should().Be(firstResponse.LabOrderStatus);

        _orderStore.Count.Should().Be(initialOrderCount);
    }

    [Fact]
    public async Task Scenario1_WorklistQuery_IncludesNewlyCreatedOrderedOrder()
    {
        // 1. Buat order dari EMR
        var cmd = CreateSampleEmrCmd("EMR-CHART-WORKLIST-01");
        var response = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);

        // 2. Kueri worklist laboratorium untuk order berstatus Ordered
        var worklist = await _worklistHandler.Handle(
            new LabOrderWorklistQuery(
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered,
                SearchTerm: null,
                Date1: null,
                Date2: null),
            CancellationToken.None);

        // Assert: order muncul pada worklist laboratorium
        worklist.Should().NotBeEmpty();
        var item = worklist.FirstOrDefault(w => w.OrderId == response.OrderId);
        item.Should().NotBeNull();
        item!.OrderNo.Should().Be(response.OrderNo);
        item.PatientName.Should().Be("Tn. Ahmad Sobari");
        item.PatientId.Should().Be("RM-9901");
        item.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        item.TestNames.Should().Contain("Darah Lengkap");
    }

    #endregion

    #region Skenario 2: Peringatan Duplikasi 24 Jam & Konfirmasi Override DPJP

    [Fact]
    public async Task Scenario2_DuplicateWithin24Hours_WithoutConfirmation_ThrowsDuplicateWarning_AndMiddlewareRenders409JSend()
    {
        // Arrange: pasang deteksi order aktif dengan tes yang sama dalam 24 jam terakhir
        var activeDuplicateTests = new List<LabOrderActiveTestDto>
        {
            new(
                OrderId: "LBO-PREV-001",
                OrderNo: "LAB2611020001",
                TestDefinitionId: "LTD-TRF-CBC",
                LabTestCode: "TRF-CBC",
                LabTestName: "Darah Lengkap",
                OrderDate: FixedNow.AddHours(-3),
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered)
        };

        _labOrderDalMock.Setup(d => d.ListActiveTestsByPatientWithinHours("RM-9901", 24, It.IsAny<DateTime>()))
            .Returns(activeDuplicateTests);

        var cmd = CreateSampleEmrCmd("EMR-CHART-DUP-01", "TRF-CBC", "Darah Lengkap", allowDuplicate: false);

        // Act: pemesanan tanpa konfirmasi override
        var act = async () => await _createFromEmrHandler.Handle(cmd, CancellationToken.None);

        // Assert Exception
        var exceptionAssertion = await act.Should().ThrowAsync<DuplicateLabOrderWarningException>();
        var exception = exceptionAssertion.Which;
        exception.DuplicateTestNames.Should().Contain("Darah Lengkap");
        exception.PreviousOrderIds.Should().Contain("LAB2611020001");

        // Assert Middleware mapping ke HTTP 409 JSend
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var middleware = new ErrorHandlerMiddleware(
            _ => throw exception,
            NullLogger<ErrorHandlerMiddleware>.Instance);

        await middleware.Invoke(httpContext);

        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        httpContext.Response.ContentType.Should().StartWith("application/json");

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseJson = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseJson);
        var root = jsonDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("fail");
        root.GetProperty("code").GetString().Should().Be("DUPLICATE_LAB_ORDER_WARNING");
        root.GetProperty("message").GetString().Should().Contain("Peringatan duplikasi tes laboratorium dalam 24 jam terakhir");

        var duplicateTestNames = root.GetProperty("data").GetProperty("duplicateTestNames")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        duplicateTestNames.Should().Contain("Darah Lengkap");

        var previousOrderIds = root.GetProperty("data").GetProperty("previousOrderIds")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        previousOrderIds.Should().Contain("LAB2611020001");
    }

    [Fact]
    public async Task Scenario2_DuplicateWithin24Hours_WithConfirmationOverride_SuccessfullyCreatesOrder()
    {
        // Arrange: terdapat tes yang sama dalam 24 jam terakhir
        var activeDuplicateTests = new List<LabOrderActiveTestDto>
        {
            new(
                OrderId: "LBO-PREV-002",
                OrderNo: "LAB2611020002",
                TestDefinitionId: "LTD-TRF-CBC",
                LabTestCode: "TRF-CBC",
                LabTestName: "Darah Lengkap",
                OrderDate: FixedNow.AddHours(-2),
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered)
        };

        _labOrderDalMock.Setup(d => d.ListActiveTestsByPatientWithinHours("RM-9901", 24, It.IsAny<DateTime>()))
            .Returns(activeDuplicateTests);

        // Act: dokter menyetujui konfirmasi override (AllowDuplicateConfirmation = true)
        var overrideCmd = CreateSampleEmrCmd("EMR-CHART-DUP-OVERRIDE", "TRF-CBC", "Darah Lengkap", allowDuplicate: true);
        var response = await _createFromEmrHandler.Handle(overrideCmd, CancellationToken.None);

        // Assert: order kedua berhasil dibuat
        response.Should().NotBeNull();
        response.OrderNo.Should().NotBeNullOrWhiteSpace();
        response.EmrOrderId.Should().Be("EMR-CHART-DUP-OVERRIDE");
        response.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);

        _orderStore.Should().ContainKey(response.OrderId);
        _orderStore[response.OrderId].OrderNo.Should().Be(response.OrderNo);
    }

    #endregion

    #region Skenario 3: Siklus Hidup & Proteksi Pembatalan

    [Fact]
    public async Task Scenario3_CancelOrder_WhenStatusIsOrdered_CancelsSuccessfullyWithReason()
    {
        // 1. Buat order dari EMR (status Ordered = 1)
        var cmd = CreateSampleEmrCmd("EMR-CHART-CANCEL-01");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);

        // 2. Dokter membatalkan order sebelum sampling
        const string reason = "Pasien pulang paksa dan menolak pemeriksaan";
        var cancelCmd = new LabOrderCancelCmd(order.OrderId, "DOKTER01", reason);
        await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        // 3. Assert status order di repository menjadi Cancelled
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be(reason);
        order.CancelledUserId.Should().Be("DOKTER01");

        // 4. Verifikasi kueri status EMR mencerminkan pembatalan
        var status = await _getByEmrOrderIdHandler.Handle(
            new LabOrderGetByEmrOrderIdQuery("EMR-CHART-CANCEL-01"), CancellationToken.None);

        status.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Cancelled);
        status.CancelledReason.Should().Be(reason);
    }

    [Fact]
    public async Task Scenario3_CancelOrder_WhenStatusIsDeferred_CancelsSuccessfully()
    {
        // 1. Buat order dan tunda (status Deferred = 2)
        var cmd = CreateSampleEmrCmd("EMR-CHART-CANCEL-DEF-01");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        order.Defer("Pasien belum berpuasa 10 jam", FixedNow.AddDays(1), "DOKTER01", FixedNow);
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Deferred);

        // 2. Batalkan order yang ditunda
        const string reason = "DPJP mengganti rencana terapi pasien";
        var cancelCmd = new LabOrderCancelCmd(order.OrderId, "DOKTER01", reason);
        await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        // 3. Assert status menjadi Cancelled
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be(reason);
    }

    [Fact]
    public async Task Scenario3_CancelOrder_WhenSpecimenAlreadyCollected_BlocksCancellationWithSpecimenGuard()
    {
        // 1. Buat order dan simulasikan alur hingga spesimen diambil (Collected = 4)
        var cmd = CreateSampleEmrCmd("EMR-CHART-GUARD-01");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        // Lakukan charge dan pengambilan sampel
        order.Charge("KASIR01");
        order.MarkCharged("TDK-1001", "KASIR01", FixedNow);
        order.CollectSpecimen(
            "PHLEBOTOMIST01",
            new CollectionInfoType(FixedNow, "PHLEBOTOMIST01", "Spesimen tabung EDTA 3ml berhasil diambil"),
            FixedNow);

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Collected);

        // 2. Upaya pembatalan dari dokter EMR setelah spesimen diambil
        var cancelCmd = new LabOrderCancelCmd(order.OrderId, "DOKTER01", "Pasien ingin membatalkan");
        var act = async () => await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        // 3. Assert domain rule memblokir pembatalan
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Collected).");

        // Status tetap Collected, tidak berubah
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Collected);
    }

    [Fact]
    public async Task Scenario3_CancelOrder_WhenStatusIsRecordedOrVerified_BlocksCancellationWithSpecimenGuard()
    {
        // 1. Order telah sampai tahap verifikasi hasil laboratorium (status Verified = 6)
        var cmd = CreateSampleEmrCmd("EMR-CHART-GUARD-02");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        order.Charge("KASIR01");
        order.MarkCharged("TDK-1002", "KASIR01", FixedNow);
        order.CollectSpecimen(
            "PHLEBOTOMIST01",
            new CollectionInfoType(FixedNow, "PHLEBOTOMIST01", "Sampling sukses"),
            FixedNow);
        order.MarkRecorded("ANALIS01", FixedNow.AddMinutes(15));
        order.MarkVerified("DOKTER_PATOLOGI", FixedNow.AddMinutes(30));

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Verified);

        // 2. Upaya pembatalan
        var cancelCmd = new LabOrderCancelCmd(order.OrderId, "DOKTER01", "Ingin membatalkan order");
        var act = async () => await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        // 3. Assert pembatalan ditolak
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Verified).");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Verified);
    }

    #endregion

    #region Skenario 4: Penerusan Otomatis ke LIS / OWARE saat Charged

    [Fact]
    public async Task Scenario4_ChargeOrder_AdvancesToCharged_CreatesOwareQueueItem_WithOfficialOrderNo()
    {
        // 1. Buat order dari EMR
        var cmd = CreateSampleEmrCmd("EMR-CHART-OWARE-01");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        var officialOrderNo = order.OrderNo;

        // 2. Charge order melalui LabOrderChargeHandler
        var chargeCmd = new LabOrderChargeCmd(order.OrderId, "KASIR01");
        var chargeResponse = await _chargeHandler.Handle(chargeCmd, CancellationToken.None);

        // 3. Assert charge sukses & status order Charged
        chargeResponse.Success.Should().BeTrue();
        chargeResponse.BillingTindakanId.Should().Be("TDK-" + officialOrderNo);
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        order.OwareStatus.Should().Be(OwareStatusEnum.Pending);

        // 4. Assert antrean OWARE terbentuk pada BILRG_LabOwareOutboundQueue
        var queueItem = _queueStore.Values.FirstOrDefault(q => q.OrderId == order.OrderId);
        queueItem.Should().NotBeNull();
        queueItem!.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Pending);
        queueItem.MessageType.Should().Be(LabOwareOutboundQueueModel.DefaultMessageType);
        queueItem.PayloadJson.Should().Contain(order.OrderId);
    }

    [Fact]
    public async Task Scenario4_OwareQueueProcessor_DispatchesToOware_UpdatesQueueSucceededAndOrderOwareSent()
    {
        // 1. Buat order dan proses charge
        var cmd = CreateSampleEmrCmd("EMR-CHART-OWARE-02");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        await _chargeHandler.Handle(new LabOrderChargeCmd(order.OrderId, "KASIR01"), CancellationToken.None);
        var queueItem = _queueStore.Values.First(q => q.OrderId == order.OrderId);

        // 2. Jalankan background processor OWARE
        var processResult = _owareQueueProcessor.ProcessOne(queueItem.QueueId, LabOwareQueueProcessor.WorkerUserId);

        // 3. Assert pengiriman ke OWARE sukses
        processResult.Success.Should().BeTrue();
        _owareIntegrationMock.Verify(o => o.Send(queueItem.PayloadJson), Times.Once);

        // Queue status menjadi Succeeded
        queueItem.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Succeeded);

        // Order OwareStatus menjadi Sent
        order.OwareStatus.Should().Be(OwareStatusEnum.Sent);
    }

    [Fact]
    public async Task Scenario4_CancelChargedOrder_EnqueuesLabOrderCancelQueue_WhenOwareWasSent()
    {
        // 1. Order sudah charged dan terkirim ke OWARE (OwareStatus == Sent)
        var cmd = CreateSampleEmrCmd("EMR-CHART-OWARE-CANCEL-01");
        var createResponse = await _createFromEmrHandler.Handle(cmd, CancellationToken.None);
        var order = _orderStore[createResponse.OrderId];

        await _chargeHandler.Handle(new LabOrderChargeCmd(order.OrderId, "KASIR01"), CancellationToken.None);
        var createQueue = _queueStore.Values.First(q => q.OrderId == order.OrderId);

        _owareQueueProcessor.ProcessOne(createQueue.QueueId, LabOwareQueueProcessor.WorkerUserId);
        order.OwareStatus.Should().Be(OwareStatusEnum.Sent);

        // 2. Sebelum spesimen diambil, terjadi pembatalan medis (misal kasir refund dan DPJP cancel)
        const string reason = "Pasien tidak bersedia melanjutkan pemeriksaan";
        var cancelCmd = new LabOrderCancelCmd(order.OrderId, "DOKTER01", reason);
        await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        // 3. Assert koordinasi TRK-BILLING dan status order
        _billingIntegrationMock.Verify(b => b.CancelCharge(
            order.OrderId,
            order.Patient.RegId,
            "DOKTER01",
            reason,
            It.IsAny<DateTime>()), Times.Once);

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);

        // 4. Assert antrean pembatalan LabOrderCancel terbentuk untuk diteruskan ke OWARE/LIS
        var cancelQueue = _queueStore.Values.FirstOrDefault(q =>
            q.OrderId == order.OrderId && q.MessageType == "LabOrderCancel");

        cancelQueue.Should().NotBeNull();
        cancelQueue!.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Pending);
        cancelQueue.PayloadJson.Should().Contain(order.OrderId);
    }

    #endregion
}
