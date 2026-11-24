using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanQueryTest
{
    private readonly InMemoryPenjualanRepo _repo = new();

    private PenjualanModel CreateSampleModel(
        string penjualanId,
        DateTime date,
        string regId = "REG001",
        string pasienName = "Pasien Uji",
        PenjualanStatusEnum status = PenjualanStatusEnum.Issued,
        bool isVoid = false)
    {
        var item = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: $"{penjualanId}001",
            brgId: "BRG001",
            brgName: "Paracetamol 500mg",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10,
            hargaSatuan: 1000m,
            diskon: 0m,
            biaya: 2000m,
            taxProsen: 0m,
            taxRupiah: 0m,
            fee: 0m);

        var model = PenjualanModel.Rehydrate(
            penjualanId: penjualanId,
            penjualanDate: date,
            penjualanTime: date.ToString("HH:mm:ss"),
            petugasId: "USER01",
            jenisPenjualan: JenisPenjualanEnum.PrescriptionBased,
            status: isVoid ? PenjualanStatusEnum.Cancelled : status,
            resepId: "RSP001",
            layananResepId: "POLI01",
            dokterId: "DOK001",
            layananId: "APT01",
            regId: regId,
            pasienName: pasienName,
            pasienAlamat: "Jl. Merdeka No. 1",
            pasienAlamat2: "",
            pasienKota: "Pekalongan",
            sex: "L",
            tipeJaminanId: "00000",
            kelasId: "3",
            sumSubTotal: 10000m,
            sumBiaya: 2000m,
            sumTaxRupiah: 0m,
            subTotal: 12000m,
            diskonLain: 0m,
            biayaLain: 0m,
            pembulatan: 0m,
            bulat: 0m,
            grandTotal: 12000m,
            kas: 0m,
            bank: 0m,
            kartuKredit: 0m,
            sisa: 12000m,
            trsKwitansiId: "",
            trsChargeId: "",
            trsMutasiId: "",
            tglSerah: new DateTime(3000, 1, 1),
            jamSerah: "00:00:00",
            petugasSerah: "",
            isVoid: isVoid,
            tglVoid: isVoid ? date : new DateTime(3000, 1, 1),
            jamVoid: isVoid ? "12:00:00" : "00:00:00",
            petugasVoid: isVoid ? "USER01" : "",
            voidReason: isVoid ? "Alasan Void" : "",
            items: new[] { item },
            version: 1);

        _repo.SaveChanges(model);
        return model;
    }

    [Fact]
    public async Task GetById_returns_detail_response_when_found()
    {
        // Arrange
        var date = new DateTime(2026, 10, 18, 9, 30, 0);
        var seeded = CreateSampleModel("DU2730001A", date, "REG100", "Ahmad Fauzi");
        var sut = new PenjualanGetByIdHandler(_repo);

        // Act
        var result = await sut.Handle(new PenjualanGetByIdQry("DU2730001A"), default);

        // Assert
        result.Should().NotBeNull();
        result!.PenjualanId.Should().Be("DU2730001A");
        result.RegId.Should().Be("REG100");
        result.PasienName.Should().Be("Ahmad Fauzi");
        result.GrandTotal.Should().Be(12000m);
        result.Items.Should().HaveCount(1);
        result.Items[0].BrgId.Should().Be("BRG001");
        result.Items[0].HargaSatuan.Should().Be(1000m);
        result.Items[0].Total.Should().Be(12000m);
    }

    [Fact]
    public async Task GetById_returns_null_when_not_found()
    {
        // Arrange
        var sut = new PenjualanGetByIdHandler(_repo);

        // Act
        var result = await sut.Handle(new PenjualanGetByIdQry("DU9999999A"), default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListByReg_returns_all_active_sales_for_registration()
    {
        // Arrange
        var date1 = new DateTime(2026, 10, 18, 8, 0, 0);
        var date2 = new DateTime(2026, 10, 18, 10, 0, 0);

        CreateSampleModel("DU2730001A", date1, "REG200", "Siti Aminah");
        CreateSampleModel("DU2730002A", date2, "REG200", "Siti Aminah");
        CreateSampleModel("DU2730003A", date2, "REG200", "Siti Aminah", isVoid: true); // Voided
        CreateSampleModel("DU2730004A", date2, "REG300", "Other Patient"); // Other Reg

        var sut = new PenjualanListByRegHandler(_repo);

        // Act
        var result = await sut.Handle(new PenjualanListByRegQry("REG200"), default);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.PenjualanId).Should().Contain("DU2730001A", "DU2730002A");
        result.Select(x => x.PenjualanId).Should().NotContain("DU2730003A");
        result.Select(x => x.PenjualanId).Should().NotContain("DU2730004A");
    }

    [Fact]
    public async Task Worklist_returns_sales_filtered_by_business_date_and_status()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 18);
        var yesterday = new DateOnly(2026, 10, 17);

        CreateSampleModel("DU2730010A", today.ToDateTime(new TimeOnly(8, 0)), status: PenjualanStatusEnum.Issued);
        CreateSampleModel("DU2730011A", today.ToDateTime(new TimeOnly(9, 0)), status: PenjualanStatusEnum.FinanciallyCleared);
        CreateSampleModel("DU2730012A", today.ToDateTime(new TimeOnly(10, 0)), status: PenjualanStatusEnum.Issued, isVoid: true); // Voided
        CreateSampleModel("DU2730013A", yesterday.ToDateTime(new TimeOnly(11, 0)), status: PenjualanStatusEnum.Issued); // Yesterday

        var sut = new PenjualanWorklistHandler(_repo);

        // Act 1: Worklist without status filter
        var allToday = await sut.Handle(new PenjualanWorklistQry(today), default);

        // Assert 1
        allToday.Should().HaveCount(2);
        allToday.Select(x => x.PenjualanId).Should().Contain("DU2730010A", "DU2730011A");

        // Act 2: Worklist with status filter (Issued = 2)
        var issuedOnly = await sut.Handle(new PenjualanWorklistQry(today, StatusFilter: (int)PenjualanStatusEnum.Issued), default);

        // Assert 2
        issuedOnly.Should().HaveCount(1);
        issuedOnly[0].PenjualanId.Should().Be("DU2730010A");
    }
}
