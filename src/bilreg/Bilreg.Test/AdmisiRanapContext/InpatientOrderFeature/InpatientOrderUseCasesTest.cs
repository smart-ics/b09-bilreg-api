using Bilreg.Api.Controllers.AdmisiRanapContext;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderUseCasesTest
{
    private readonly Mock<IInpatientOrderRepo> _orderRepo = new();
    private readonly Mock<IPatientAdministrationGateway> _patientGateway = new();
    private readonly Mock<IDoctorServiceGateway> _doctorGateway = new();
    private readonly Mock<IWardAccommodationGateway> _wardGateway = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<IPasienTrackerInpatientCoordinator> _tracker = new();

    public InpatientOrderUseCasesTest()
    {
        _patientGateway.Setup(x => x.ResolvePatient("P01"))
            .Returns(new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"));
        _doctorGateway.Setup(x => x.ResolveDoctor("D01"))
            .Returns(new PpaReff("D01", "Dr. Dokter"));
        _wardGateway.Setup(x => x.ResolveKelas("K01"))
            .Returns(new KelasReff("K01", "Kelas VIP"));
        _wardGateway.Setup(x => x.ResolveBangsal("B01"))
            .Returns(new BangsalReff("B01", "Bangsal Anggrek"));
        _tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(TipeJaminanType.Default with { TipeJaminanId = "TJ01", TipeJaminanName = "BPJS" }));
    }

    [Fact]
    public async Task MaintainReservation_WhenStatusRequested_UpdatesSuccessfully()
    {
        var existing = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));
        _wardGateway.Setup(x => x.ResolveKelas("K02")).Returns(new KelasReff("K02", "Kelas 1"));
        _wardGateway.Setup(x => x.ResolveBangsal("B02")).Returns(new BangsalReff("B02", "Bangsal Melati"));

        var handler = new AdmMaintainReservationHandler(
            _orderRepo.Object, _wardGateway.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        var cmd = new AdmMaintainReservationCmd(
            existing.InpatientOrderId, "2026-11-15", "K02", "B02", "editor");

        await handler.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<InpatientOrderModel>(o =>
            o.InpatientOrderId == existing.InpatientOrderId &&
            o.OrderStatus == InpatientOrderStatusEnum.Maintained &&
            o.KelasRawat.KelasId == "K02" &&
            o.Bangsal.BangsalId == "B02" &&
            o.PlannedDate == new DateTime(2026, 11, 15))), Times.Once);
    }

    [Fact]
    public async Task CancelInpatientOrder_WhenValid_SetsStatusCancelledAndAudits()
    {
        var existing = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));

        var handler = new AdmCancelInpatientOrderHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        var cmd = new AdmCancelInpatientOrderCmd(existing.InpatientOrderId, "canceller", "Alasan pembatalan valid");
        await handler.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<InpatientOrderModel>(o =>
            o.InpatientOrderId == existing.InpatientOrderId &&
            o.OrderStatus == InpatientOrderStatusEnum.Cancelled)), Times.Once);

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "VOID" &&
            a.Reason == "Alasan pembatalan valid")), Times.Once);
    }

    [Fact]
    public async Task CancelInpatientOrder_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var existing = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1").Cancel("canceller1");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));

        var handler = new AdmCancelInpatientOrderHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        var cmd = new AdmCancelInpatientOrderCmd(existing.InpatientOrderId, "canceller2", "Alasan kedua");
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetInpatientOrderInsurance_WhenValid_UpdatesFlatInsurance()
    {
        var existing = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));

        var handler = new AdmSetInpatientOrderInsuranceHandler(
            _orderRepo.Object, _tipeJaminanRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        var cmd = new AdmSetInpatientOrderInsuranceCmd(existing.InpatientOrderId, "TJ01", "SEP-999", "admin");
        await handler.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<InpatientOrderModel>(o =>
            o.Insurance.TipeJaminan.TipeJaminanId == "TJ01" &&
            o.Insurance.JaminanReffId == "SEP-999")), Times.Once);
    }

    [Fact]
    public async Task GetOpnameRequestQry_WhenFound_ReturnsMappedResponse()
    {
        var existing = InpatientOrderModel.CreateEmr(
            OrderSourceEnum.EmrRawatJalan,
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new PpaReff("D01", "Dr. Dokter"),
            new DateTime(2026, 11, 10),
            "Catatan klinis",
            "user1",
            "EXT-100",
            "EMR_RJ");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));

        var handler = new AdmGetOpnameRequestHandler(_orderRepo.Object);
        var response = await handler.Handle(new AdmGetOpnameRequestQry(existing.InpatientOrderId), CancellationToken.None);

        response.OpnameRequestId.Should().Be(existing.InpatientOrderId);
        response.DokterId.Should().Be("D01");
        response.ClinicalNotes.Should().Be("Catatan klinis");
        response.TrsOrderId.Should().Be("EXT-100");
        response.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Requested);
    }

    [Fact]
    public async Task GetReservationQry_WhenFound_ReturnsMappedResponse()
    {
        var existing = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.GetByKey(existing.InpatientOrderId)).Returns(MayBe.From(existing));

        var handler = new AdmGetReservationHandler(_orderRepo.Object);
        var response = await handler.Handle(new AdmGetReservationQry(existing.InpatientOrderId), CancellationToken.None);

        response.ReservationId.Should().Be(existing.InpatientOrderId);
        response.KelasRawat.KelasId.Should().Be("K01");
        response.Bangsal.BangsalId.Should().Be("B01");
        response.ReservationStatus.Should().Be(ReservationStatusEnum.Reserved);
    }

    [Fact]
    public async Task ListOpnameRequestQry_FiltersByEmrSource()
    {
        var emrOrder = InpatientOrderModel.CreateEmr(
            OrderSourceEnum.EmrRawatJalan,
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new PpaReff("D01", "Dr. Dokter"),
            new DateTime(2026, 11, 10),
            "Catatan", "user1");

        var bookingOrder = InpatientOrderModel.CreateBooking(
            new PasienReff("P02", "Pasien B", new DateOnly(1990, 1, 1), "P"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.ListData(It.IsAny<InpatientOrderListFilter>()))
            .Returns([emrOrder, bookingOrder]);

        var handler = new AdmListOpnameRequestHandler(_orderRepo.Object);
        var result = await handler.Handle(new AdmListOpnameRequestQry(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].OpnameRequestId.Should().Be(emrOrder.InpatientOrderId);
    }

    [Fact]
    public async Task ListReservationQry_FiltersByBookingSource()
    {
        var emrOrder = InpatientOrderModel.CreateEmr(
            OrderSourceEnum.EmrRawatJalan,
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new PpaReff("D01", "Dr. Dokter"),
            new DateTime(2026, 11, 10),
            "Catatan", "user1");

        var bookingOrder = InpatientOrderModel.CreateBooking(
            new PasienReff("P02", "Pasien B", new DateOnly(1990, 1, 1), "P"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.ListData(It.IsAny<InpatientOrderListFilter>()))
            .Returns([emrOrder, bookingOrder]);

        var handler = new AdmListReservationHandler(_orderRepo.Object);
        var result = await handler.Handle(new AdmListReservationQry(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ReservationId.Should().Be(bookingOrder.InpatientOrderId);
    }

    [Fact]
    public async Task OpnameRequestController_Create_DelegatesToMediatorWithAgnosticFields()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<AdmCreateOpnameRequestCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmCreateOpnameRequestResponse("OPN000000001", "EXT-123"));

        var controller = new OpnameRequestController(mediatorMock.Object);
        var postModel = new AdmCreateOpnameRequestPostModel(
            "P01", "D01", "2026-11-10", "Notes", "user1",
            ExternalReferenceId: "EXT-123", ExternalSystemSource: "SATUSEHAT", OrderSource: OrderSourceEnum.EmrIgd);

        var actionResult = await controller.Create(postModel);
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var jsend = okResult!.Value as JSendOk;
        jsend.Should().NotBeNull();

        mediatorMock.Verify(m => m.Send(It.Is<AdmCreateOpnameRequestCmd>(c =>
            c.PasienId == "P01" &&
            c.DokterId == "D01" &&
            c.ExternalReferenceId == "EXT-123" &&
            c.ExternalSystemSource == "SATUSEHAT" &&
            c.OrderSource == OrderSourceEnum.EmrIgd), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReservationController_CreateAndMaintainAndCancel_DelegatesToMediator()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<AdmCreateReservationCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmCreateReservationResponse("OPN000000002"));

        var controller = new ReservationController(mediatorMock.Object);

        // Create
        var createResult = await controller.Create(new AdmCreateReservationCmd("P01", "2026-11-10", "K01", "B01", "user1"));
        (createResult as OkObjectResult).Should().NotBeNull();

        // Maintain
        var maintainResult = await controller.Maintain("OPN000000002", new AdmMaintainReservationBody("2026-11-15", "K02", "B02", "user2"));
        (maintainResult as OkObjectResult).Should().NotBeNull();

        // Cancel
        var cancelResult = await controller.Cancel("OPN000000002", new AdmCancelReservationBody("user2", "Batal"));
        (cancelResult as OkObjectResult).Should().NotBeNull();

        mediatorMock.Verify(m => m.Send(It.Is<AdmCancelInpatientOrderCmd>(c =>
            c.InpatientOrderId == "OPN000000002" && c.Reason == "Batal"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReservationController_History_DelegatesToMediator()
    {
        var mediatorMock = new Mock<IMediator>();
        var expectedResponse = new AdmReservationHistoryResponse(
            "OPN000000002", "P01", "Pasien A", new DateTime(2026, 11, 15), "Kelas 1", "Bangsal B",
            1, "user1", new DateTime(2026, 11, 1), "user2", new DateTime(2026, 11, 5), []);

        mediatorMock.Setup(m => m.Send(It.Is<AdmGetReservationHistoryQry>(q => q.ReservationId == "OPN000000002"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new ReservationController(mediatorMock.Object);
        var result = await controller.History("OPN000000002");
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var jsend = okResult!.Value as JSendOk;
        jsend.Should().NotBeNull();
        mediatorMock.Verify(m => m.Send(It.Is<AdmGetReservationHistoryQry>(q => q.ReservationId == "OPN000000002"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdmGetReservationHistoryHandler_ReturnsEntriesWithAuditAndCreation()
    {
        var order = InpatientOrderModel.CreateBooking(
            new PasienReff("P01", "Pasien A", new DateOnly(1985, 3, 10), "L"),
            new DateTime(2026, 11, 10),
            new KelasReff("K01", "Kelas VIP"),
            new BangsalReff("B01", "Bangsal Anggrek"),
            "user1");

        _orderRepo.Setup(x => x.GetByKey(order.InpatientOrderId)).Returns(MayBe.From(order));
        _orderRepo.Setup(x => x.ListAudits(order.InpatientOrderId)).Returns(new List<InpatientOrderAuditRecord>
        {
            new("AUD01", new DateTime(2026, 11, 5, 10, 0, 0), "user2", "UPDATE", "Rescheduled by request", "{\"PlannedDate\":\"2026-11-10T00:00:00\"}")
        });

        var handler = new AdmGetReservationHistoryHandler(_orderRepo.Object);
        var response = await handler.Handle(new AdmGetReservationHistoryQry(order.InpatientOrderId), CancellationToken.None);

        response.Should().NotBeNull();
        response.ReservationId.Should().Be(order.InpatientOrderId);
        response.Entries.Should().HaveCount(2);
        response.Entries[0].ActionType.Should().Be("CREATE");
        response.Entries[1].ActionType.Should().Be("UPDATE");
        response.Entries[1].PreviousPlannedDate.Should().Be(new DateTime(2026, 11, 10));
        response.Entries[1].Notes.Should().Be("Rescheduled by request");
    }
}
