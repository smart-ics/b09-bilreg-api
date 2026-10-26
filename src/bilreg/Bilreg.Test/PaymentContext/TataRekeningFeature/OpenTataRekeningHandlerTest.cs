using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class OpenTataRekeningHandlerTest
{
    private const string RegId = "REG-OPEN-001";
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IMergeRequestRepo> _mergeRequestRepo = new();
    private readonly Mock<IDepositRepo> _depositRepo = new();

    private readonly OpenTataRekeningHandler _sut;

    public OpenTataRekeningHandlerTest()
    {
        _sut = new OpenTataRekeningHandler(
            _tataRekeningRepo.Object,
            _regRepo.Object,
            _mergeRequestRepo.Object,
            _depositRepo.Object);
    }

    [Fact]
    public async Task Handle_GivenValidRegAndBills_ReturnsCompositeResponseWithPatientContextAndSummaryBreakdown()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Budi Santoso", new DateOnly(1985, 5, 12), "L");
        var layanan = new LayananReff("LAY-01", "Rawat Inap Melati");
        var dokter = new PpaReff("DOK-01", "dr. Andi, Sp.PD");
        var kelas = new KelasReff("KLS-01", "Kelas 1");
        var jaminan = new TipeJaminanReff("JAM-01", "BPJS Kesehatan");

        var reg = new RegModel(
            RegId,
            new DateOnly(2026, 6, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap,
            pasien, jaminan, PolisModel.Default.ToReff(), kelas,
            CaraMasukDkType.Default, RujukanType.Default.ToReff(),
            dokter, layanan, KarcisType.Default.ToReff(),
            RegEligibilityType.Default, []);

        var bill1 = CreateBillWithComponents(
            "BILL-JASA-01",
            RegId,
            BillModulGroup.Jasa,
            subTotal: 200_000m,
            diskon: 20_000m,
            tax: 10_000m,
            biaya: 5_000m);

        var bill2 = CreateBillWithComponents(
            "BILL-OBAT-01",
            RegId,
            BillModulGroup.Obat,
            subTotal: 100_000m,
            diskon: 10_000m,
            tax: 5_000m,
            biaya: 2_000m);

        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            listTataRekeningPayment: [],
            listTrsBill: [bill1, bill2]);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));
        _mergeRequestRepo.Setup(r => r.ListPendingByReg(It.IsAny<IRegKey>()))
            .Returns([]);
        _mergeRequestRepo.Setup(r => r.ListPendingByPatient(It.IsAny<string>()))
            .Returns([]);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 100_000m, 0m, 100_000m));

        // Act
        var result = await _sut.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        // Assert - PatientContext
        result.PatientContext.Should().NotBeNull();
        result.PatientContext!.PasienId.Should().Be("PAS-001");
        result.PatientContext.PasienName.Should().Be("Budi Santoso");
        result.PatientContext.Gender.Should().Be("L");
        result.PatientContext.TglLahir.Should().Be("1985-05-12");
        result.PatientContext.LayananName.Should().Be("Rawat Inap Melati");
        result.PatientContext.JenisReg.Should().Be("RegInap");
        result.PatientContext.TipeJaminanId.Should().Be("JAM-01");
        result.PatientContext.TipeJaminanName.Should().Be("BPJS Kesehatan");
        result.PatientContext.KelasName.Should().Be("Kelas 1");
        result.PatientContext.DpjpName.Should().Be("dr. Andi, Sp.PD");
        result.PatientContext.AdvancePayment.Should().Be(0m);
        result.PatientContext.DepositPayment.Should().Be(100_000m);

        // Assert - SummaryBreakdown
        result.SummaryBreakdown.Should().NotBeNull();
        result.SummaryBreakdown!.TagihanLalu.Jasa.Should().Be(0m);
        result.SummaryBreakdown.TagihanLalu.Obat.Should().Be(0m);
        result.SummaryBreakdown.TagihanLalu.Total.Should().Be(0m);

        result.SummaryBreakdown.TagihanSaatIni.Jasa.Should().Be(200_000m);
        result.SummaryBreakdown.TagihanSaatIni.Obat.Should().Be(100_000m);
        result.SummaryBreakdown.TagihanSaatIni.Diskon.Should().Be(30_000m);
        result.SummaryBreakdown.TagihanSaatIni.Pajak.Should().Be(15_000m);
        result.SummaryBreakdown.TagihanSaatIni.Total.Should().Be(285_000m);

        result.SummaryBreakdown.BiayaAdministrasi.Administrasi.Should().Be(7_000m);
        result.SummaryBreakdown.BiayaAdministrasi.Meterai.Should().Be(0m);
        result.SummaryBreakdown.BiayaAdministrasi.Pembulatan.Should().Be(0m);
        result.SummaryBreakdown.BiayaAdministrasi.Total.Should().Be(7_000m);

        result.SummaryBreakdown.GrandTotalKewajiban.Should().Be(292_000m);
        // SisaTagihanKasir = 292_000 - 100_000 (deposit) = 192_000m
        result.SummaryBreakdown.SisaTagihanKasir.Should().Be(192_000m);
    }

    [Fact]
    public async Task Handle_GivenExistingAllocationsInListPayment_CalculatesSisaTagihanKasirFromAllocations()
    {
        // Arrange
        var reg = RegModel.Default;
        var bill = CreateBillWithComponents(
            "BILL-JASA-02",
            RegId,
            BillModulGroup.Jasa,
            subTotal: 300_000m,
            diskon: 0m,
            tax: 0m,
            biaya: 0m);

        var paymentDpu = new TataRekeningPaymentType(
            PaymentType.ByDpu, 100_000m, 0m, new CoaType("COA-DPU", "Deposit"));
        var paymentBpjs = new TataRekeningPaymentType(
            new PaymentType("JAM-BPJS", "BPJS", true), 150_000m, 0m, new CoaType("COA-BPJS", "BPJS"));
        var paymentKas = new TataRekeningPaymentType(
            PaymentType.ByKas, 50_000m, 0m, new CoaType("COA-KAS", "Kas"));

        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Closed,
            TataRekeningFinalizationType.Default,
            listTataRekeningPayment: [paymentDpu, paymentBpjs, paymentKas],
            listTrsBill: [bill]);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));
        _mergeRequestRepo.Setup(r => r.ListPendingByReg(It.IsAny<IRegKey>()))
            .Returns([]);
        _mergeRequestRepo.Setup(r => r.ListPendingByPatient(It.IsAny<string>()))
            .Returns([]);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 100_000m, 100_000m, 0m));

        // Act
        var result = await _sut.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        // Assert
        result.SummaryBreakdown.Should().NotBeNull();
        result.SummaryBreakdown!.GrandTotalKewajiban.Should().Be(300_000m);
        // Non-kasir allocation = 100_000 (DPU) + 150_000 (BPJS) = 250_000m
        // SisaTagihanKasir = 300_000 - 250_000 = 50_000m
        result.SummaryBreakdown.SisaTagihanKasir.Should().Be(50_000m);
    }

    [Fact]
    public async Task Handle_GivenTataRekeningNotFound_ThrowsKeyNotFoundException()
    {
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe<TataRekeningModel>.None);

        var act = () => _sut.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{RegId}*");
    }

    [Fact]
    public async Task Handle_GivenRegNotFound_ThrowsKeyNotFoundException()
    {
        var tataRekening = TataRekeningModel.Create(RegId);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe<RegModel>.None);

        var act = () => _sut.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{RegId}*");
    }

    [Fact]
    public async Task Handle_GivenNullOrWhiteSpaceRegId_ThrowsArgumentException()
    {
        var act = () => _sut.Handle(new OpenTataRekeningQuery(""), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void TataRekeningApiMapper_ToApiResponse_MapsPatientContextAndSummaryBreakdown()
    {
        var summary = new TataRekeningSummaryDto(RegId, TataRekeningStatusEnum.Opened, FinancialVerificationStatusEnum.NotVerified, false, false);
        var patientContext = new TataRekeningPatientContextDto(
            "PAS-001", "Budi", "L", "1990-01-01", "Poli Penyakit Dalam", "RegJalan",
            "JAM-01", "Umum", "Kelas 3", "dr. Budi", 0m, 50_000m);
        var breakdown = new TataRekeningBillingBreakdownDto(
            new TagihanLaluComponent(0, 0, 0),
            new TagihanSaatIniComponent(100_000m, 50_000m, 5_000m, 2_000m, 147_000m),
            new BiayaAdministrasiComponent(3_000m, 0m, 0m, 3_000m),
            150_000m,
            100_000m);

        var response = new OpenTataRekeningResponse(
            summary,
            Bills: [],
            Projection: [],
            PendingMergeRequests: [],
            PatientContext: patientContext,
            SummaryBreakdown: breakdown);

        var apiResponse = TataRekeningApiMapper.ToApiResponse(response);

        apiResponse.Summary.Should().Be(summary);
        apiResponse.PatientContext.Should().Be(patientContext);
        apiResponse.SummaryBreakdown.Should().Be(breakdown);
        apiResponse.PatientContext!.PasienName.Should().Be("Budi");
        apiResponse.SummaryBreakdown!.GrandTotalKewajiban.Should().Be(150_000m);
        apiResponse.SummaryBreakdown.SisaTagihanKasir.Should().Be(100_000m);
    }

    private static TrsBillType CreateBillWithComponents(
        string billingId,
        string regId,
        BillModulGroup modulGroup,
        decimal subTotal,
        decimal diskon,
        decimal tax,
        decimal biaya)
    {
        var regReff = new RegReff(regId, "PAS-001", "Pasien Test");
        var layananReff = new LayananReff("LAY-01", "Layanan Test");
        var kelasReff = KelasType.Default.ToReff();
        var rekapCetakReff = new RekapCetakReff("RK-01", "Rekap Test");
        var nilai = new TrsBillNilaiType(subTotal, diskon, tax, biaya);
        var ket = new TrsBillKetType("Ket 1", "Ket 2", "REF-01", 1, "MAIN-01");

        return new TrsBillType(
            billingId,
            modulGroup,
            new DateTime(2026, 6, 16, 9, 0, 0),
            regReff,
            layananReff,
            kelasReff,
            AuditInfoType.Default,
            rekapCetakReff,
            nilai,
            ket,
            [], [], []);
    }
}
