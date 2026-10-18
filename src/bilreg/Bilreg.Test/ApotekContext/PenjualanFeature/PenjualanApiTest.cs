using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Nuna.Lib.AutoNumberHelper;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

[Collection(ApotekApiCollection.Name)]
public class PenjualanApiTest
{
    private readonly ApotekApiWebApplicationFactory _factory;

    public PenjualanApiTest(ApotekApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        ApotekApiTestAuthHandler.IsAuthenticated = true;
        ApotekApiTestAuthHandler.UserId = "TEST-USER";
    }

    #region Helper Seed Methods

    private RegModel SeedReg(
        string regId = "REG001",
        string tipeJaminanId = "00000",
        bool isRanap = false,
        bool isAktif = true,
        string pasienName = "Budi Santoso")
    {
        var pasienReff = new PasienReff("PAS001", pasienName, new DateOnly(1990, 1, 1), "L");
        var reg = new RegModel(
            regId,
            DateOnly.FromDateTime(DateTime.Today),
            new AuditInfoType("tester", DateTime.Today),
            isAktif ? AuditInfoType.Default : new AuditInfoType("USER", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            isRanap ? JenisRegEnum.RegInap : JenisRegEnum.RegJalan,
            pasienReff,
            new TipeJaminanReff(tipeJaminanId, tipeJaminanId == "JKN00" ? "BPJS Kesehatan" : "Umum"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DOK001", "dr. Subroto, Sp.PD"),
            new LayananReff("POLI01", "Poli Penyakit Dalam"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        _factory.Harness.RegRepo.SaveChanges(reg);
        return reg;
    }

    private ResepModel SeedResep(string resepId, RegModel reg, params (string brgId, string brgName, decimal qty)[] items)
    {
        var listObat = new List<ResepObatType>();
        int no = 1;
        foreach (var item in items)
        {
            var brg = BrgObatType.Default with { BrgId = item.brgId, BrgName = item.brgName };
            var etiket = EtiketType.Default with { Signa = "3x1", Instruction = "Sesudah makan", Note = "Habiskan" };
            var obat = ResepObatType.Create(no++, brg, SatuanType.Default, item.qty, 0, etiket);
            listObat.Add(obat);
        }

        var resep = new ResepModel(
            resepId,
            reg.ToReff(),
            BodyMetricType.Default(),
            new DokterReff(reg.Dokter.PpaId, reg.Dokter.PpaName),
            new LayananReff(reg.Layanan.LayananId, reg.Layanan.LayananName),
            UrgenitasType.Default,
            TipeBrgType.Default.ToReff(),
            0,
            "Resep Uji",
            AuditTrailType.Default,
            listObat);

        _factory.Harness.ResepRepo.SaveChanges(resep);
        return resep;
    }

    private TelaahResepModel SeedTelaah(
        string resepId,
        string regId,
        TelaahStatusEnum status,
        params (string brgId, string brgName, decimal qty, TelaahDispositionEnum disp)[] items)
    {
        var telaahItems = new List<TelaahResepItemModel>();
        int no = 1;
        foreach (var it in items)
        {
            var acceptedQty = it.disp == TelaahDispositionEnum.Rejected ? 0 : it.qty;
            telaahItems.Add(new TelaahResepItemModel(
                itemNo: no,
                resepItemNo: no++,
                disposition: it.disp,
                acceptedBrgId: it.brgId,
                acceptedBrgName: it.brgName,
                acceptedQty: acceptedQty,
                reason: it.disp == TelaahDispositionEnum.Rejected ? "Tidak tersedia" : "",
                pharmacistId: "APT01"));
        }

        var telaah = TelaahResepModel.Open(resepId, regId, telaahItems);
        if (status is TelaahStatusEnum.Approved or TelaahStatusEnum.PartiallyApproved or TelaahStatusEnum.Rejected)
        {
            telaah.Start("APT01", DateTime.Now);
            telaah.Complete(DateTime.Now);
        }

        _factory.Harness.TelaahRepo.SaveChanges(telaah);
        return telaah;
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data");
    }

    #endregion

    #region 1. Authorization Tests

    [Fact]
    public async Task API01_Unauthenticated_request_is_challenged_with_401()
    {
        try
        {
            ApotekApiTestAuthHandler.IsAuthenticated = false;
            var client = _factory.CreateUnauthenticatedClient();

            var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/from-telaah", new
            {
                resepId = "RSP001"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            ApotekApiTestAuthHandler.IsAuthenticated = true;
        }
    }

    #endregion

    #region 2. POST /api/v1/apotek/penjualan/from-telaah (Jenis 1)

    [Fact]
    public async Task API02_CreateFromTelaah_single_document_returns_DU_and_stamps_authenticated_actor()
    {
        // Arrange
        var reg = SeedReg("REG101", "00000");
        var resep = SeedResep("RSP101", reg, ("BRG01", "Amoxicillin 500mg", 10m));
        SeedTelaah(resep.ResepId, reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Amoxicillin 500mg", 10m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/from-telaah", new
        {
            resepId = resep.ResepId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);

        var penjualanId = data[0].GetString()!;
        penjualanId.Should().StartWith(PenjualanModel.IdPrefix);

        // Verify stored entity has authenticated actor stamped
        var stored = _factory.Harness.PenjualanRepo.Store[penjualanId];
        stored.PetugasId.Should().Be("TEST-USER");
        stored.ResepId.Should().Be(resep.ResepId);
        stored.RegId.Should().Be(reg.RegId);
        stored.JenisPenjualan.Should().Be(JenisPenjualanEnum.PrescriptionBased);
        stored.Items.Should().HaveCount(1);
        stored.TglSerah.Should().Be(new DateTime(3000, 1, 1));
        stored.TrsMutasiId.Should().BeEmpty();
    }

    [Fact]
    public async Task API03_CreateFromTelaah_BPJS_mixed_payer_split_returns_2_DU_documents()
    {
        // Arrange
        var reg = SeedReg("REG102", "JKN00");
        var resep = SeedResep("RSP102", reg,
            ("BRG_FORNAS", "Paracetamol", 10m),
            ("BRG_NON_FORNAS", "Suplemen Herbal", 5m));

        SeedTelaah(resep.ResepId, reg.RegId, TelaahStatusEnum.Approved,
            ("BRG_FORNAS", "Paracetamol", 10m, TelaahDispositionEnum.AcceptedAsPrescribed),
            ("BRG_NON_FORNAS", "Suplemen Herbal", 5m, TelaahDispositionEnum.AcceptedAsPrescribed));

        _factory.Harness.FornasCheckPort.NonFornasItemIds.Add("BRG_NON_FORNAS");

        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/from-telaah", new
        {
            resepId = resep.ResepId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetArrayLength().Should().Be(2);

        var bpjsId = data[0].GetString()!;
        var mandiriId = data[1].GetString()!;

        var bpjsDoc = _factory.Harness.PenjualanRepo.Store[bpjsId];
        var mandiriDoc = _factory.Harness.PenjualanRepo.Store[mandiriId];

        bpjsDoc.TipeJaminanId.Should().Be("JKN00");
        bpjsDoc.Items.Single().BrgId.Should().Be("BRG_FORNAS");

        mandiriDoc.TipeJaminanId.Should().Be("00000");
        mandiriDoc.Items.Single().BrgId.Should().Be("BRG_NON_FORNAS");
    }

    [Fact]
    public async Task API04_CreateFromTelaah_with_unapproved_status_maps_to_400_DOMAIN()
    {
        // Arrange
        var reg = SeedReg("REG103", "00000");
        var resep = SeedResep("RSP103", reg, ("BRG01", "Amoxicillin", 10m));
        SeedTelaah(resep.ResepId, reg.RegId, TelaahStatusEnum.UnderReview,
            ("BRG01", "Amoxicillin", 10m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/from-telaah", new
        {
            resepId = resep.ResepId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("fail");
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    [Fact]
    public async Task API05_CreateFromTelaah_empty_resep_maps_to_400_DOMAIN()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/from-telaah", new
        {
            resepId = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    #endregion

    #region 3. POST /api/v1/apotek/penjualan/direct (Jenis 2)

    [Fact]
    public async Task API06_CreateDirect_success_returns_DU_and_stamps_authenticated_actor()
    {
        // Arrange
        var reg = SeedReg("REG201", "00000", pasienName: "Ibu Ratna");
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/direct", new
        {
            regId = reg.RegId,
            dokterId = "DOK001",
            layananId = "APT01",
            items = new[]
            {
                new
                {
                    brgId = "BRG02",
                    brgName = "Kasa Steril",
                    satuanId = "PCS",
                    satuanName = "Pieces",
                    qty = 2m
                }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        var penjualanId = data.GetString()!;
        penjualanId.Should().StartWith(PenjualanModel.IdPrefix);

        var stored = _factory.Harness.PenjualanRepo.Store[penjualanId];
        stored.PetugasId.Should().Be("TEST-USER");
        stored.RegId.Should().Be("REG201");
        stored.PasienName.Should().Be("Ibu Ratna");
        stored.ResepId.Should().BeEmpty();
        stored.JenisPenjualan.Should().Be(JenisPenjualanEnum.RegisteredDirect);
        stored.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task API07_CreateDirect_inactive_registration_maps_to_400_DOMAIN()
    {
        // Arrange
        var reg = SeedReg("REG202", "00000", isAktif: false);
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/direct", new
        {
            regId = reg.RegId,
            dokterId = "DOK001",
            layananId = "APT01",
            items = new[]
            {
                new { brgId = "BRG02", brgName = "Kasa", satuanId = "PCS", satuanName = "Pcs", qty = 1m }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    [Fact]
    public async Task API08_CreateDirect_non_existent_reg_maps_to_404_NOT_FOUND()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/direct", new
        {
            regId = "REG_NONE",
            dokterId = "DOK001",
            layananId = "APT01",
            items = new[]
            {
                new { brgId = "BRG02", brgName = "Kasa", satuanId = "PCS", satuanName = "Pcs", qty = 1m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    #endregion

    #region 4. POST /api/v1/apotek/penjualan/walk-in (Jenis 3)

    [Fact]
    public async Task API09_CreateWalkIn_success_returns_DU_with_walk_in_invariants()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/walk-in", new
        {
            pembeliName = "Pak Hendra",
            pembeliAlamat = "Jl. Sudirman No. 45",
            layananId = "APT01",
            items = new[]
            {
                new
                {
                    brgId = "BRG03",
                    brgName = "Minyak Kayu Putih",
                    satuanId = "BTL",
                    satuanName = "Botol",
                    qty = 1m
                }
            }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        var penjualanId = data.GetString()!;
        penjualanId.Should().StartWith(PenjualanModel.IdPrefix);

        var stored = _factory.Harness.PenjualanRepo.Store[penjualanId];
        stored.PetugasId.Should().Be("TEST-USER");
        stored.PasienName.Should().Be("Pak Hendra");
        stored.PasienAlamat.Should().Be("Jl. Sudirman No. 45");
        stored.RegId.Should().BeEmpty();
        stored.ResepId.Should().BeEmpty();
        stored.TipeJaminanId.Should().Be("00000");
        stored.JenisPenjualan.Should().Be(JenisPenjualanEnum.WalkInOTC);
        stored.Items.Single().IsBeliBebas.Should().BeTrue();
    }

    [Fact]
    public async Task API10_CreateWalkIn_empty_name_maps_to_400_DOMAIN()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/walk-in", new
        {
            pembeliName = "",
            layananId = "APT01",
            items = new[]
            {
                new { brgId = "BRG03", brgName = "Item", satuanId = "PCS", satuanName = "Pcs", qty = 1m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    #endregion

    #region 5. POST /api/v1/apotek/penjualan/{penjualanId}/void

    [Fact]
    public async Task API11_Void_success_soft_voids_penjualan_and_envelopes_JSendOk()
    {
        // Arrange
        var penjualan = CreateSamplePenjualan("DU1111111A");
        _factory.Harness.PenjualanRepo.SaveChanges(penjualan);
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/apotek/penjualan/{penjualan.PenjualanId}/void", new
        {
            reason = "Salah input item obat"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetProperty("penjualanId").GetString().Should().Be(penjualan.PenjualanId);

        var stored = _factory.Harness.PenjualanRepo.Store[penjualan.PenjualanId];
        stored.IsVoid.Should().BeTrue();
        stored.PetugasVoid.Should().Be("TEST-USER");
        stored.Status.Should().Be(PenjualanStatusEnum.Cancelled);
        stored.Items.All(x => x.IsVoid).Should().BeTrue();
    }

    [Fact]
    public async Task API12_Void_empty_reason_maps_to_400_DOMAIN()
    {
        var penjualan = CreateSamplePenjualan("DU1111112A");
        _factory.Harness.PenjualanRepo.SaveChanges(penjualan);
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync($"/api/v1/apotek/penjualan/{penjualan.PenjualanId}/void", new
        {
            reason = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    [Fact]
    public async Task API13_Void_non_existent_id_maps_to_404_NOT_FOUND()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/apotek/penjualan/DU9999999A/void", new
        {
            reason = "Pembatalan"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    #endregion

    #region 6. GET /api/v1/apotek/penjualan/{penjualanId}

    [Fact]
    public async Task API14_GetById_returns_full_detail_document()
    {
        // Arrange
        var penjualan = CreateSamplePenjualan("DU2222222A");
        _factory.Harness.PenjualanRepo.SaveChanges(penjualan);
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/v1/apotek/penjualan/{penjualan.PenjualanId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetProperty("penjualanId").GetString().Should().Be(penjualan.PenjualanId);
        data.GetProperty("pasienName").GetString().Should().Be(penjualan.PasienName);
        data.GetProperty("grandTotal").GetDecimal().Should().Be(penjualan.GrandTotal);

        var items = data.GetProperty("items");
        items.GetArrayLength().Should().Be(penjualan.Items.Count);
        items[0].GetProperty("brgId").GetString().Should().Be(penjualan.Items[0].BrgId);
    }

    [Fact]
    public async Task API15_GetById_non_existent_maps_to_404_NOT_FOUND()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/apotek/penjualan/DU9999999A");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task API15b_GetById_invalid_format_maps_to_400_DOMAIN()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/apotek/penjualan/DU_INVALID_FORMAT");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DOMAIN");
    }

    #endregion

    #region 7. GET /api/v1/apotek/penjualan/by-reg/{regId}

    [Fact]
    public async Task API16_ListByReg_returns_active_sales_for_registration()
    {
        // Arrange
        var p1 = CreateSamplePenjualan("DU3333331A", regId: "REG_HIST_01");
        var p2 = CreateSamplePenjualan("DU3333332A", regId: "REG_HIST_01");
        var pVoid = CreateSamplePenjualan("DU3333333A", regId: "REG_HIST_01");
        pVoid.Void("TEST", DateTime.Now, "Batal", tataRekeningAllows: true);

        _factory.Harness.PenjualanRepo.SaveChanges(p1);
        _factory.Harness.PenjualanRepo.SaveChanges(p2);
        _factory.Harness.PenjualanRepo.SaveChanges(pVoid);

        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/apotek/penjualan/by-reg/REG_HIST_01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetArrayLength().Should().Be(2);

        var ids = new List<string> { data[0].GetProperty("penjualanId").GetString()!, data[1].GetProperty("penjualanId").GetString()! };
        ids.Should().Contain(p1.PenjualanId);
        ids.Should().Contain(p2.PenjualanId);
        ids.Should().NotContain(pVoid.PenjualanId);
    }

    #endregion

    #region 8. GET /api/v1/apotek/penjualan/worklist

    [Fact]
    public async Task API17_Worklist_returns_sales_by_business_date_with_filter()
    {
        // Arrange
        var today = DateTime.Today;
        var pToday = CreateSamplePenjualan("DU4444441A", trsDate: today);
        var pPast = CreateSamplePenjualan("DU4444442A", trsDate: today.AddDays(-2));

        _factory.Harness.PenjualanRepo.SaveChanges(pToday);
        _factory.Harness.PenjualanRepo.SaveChanges(pPast);

        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/v1/apotek/penjualan/worklist?businessDate={today:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Data(response);
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("penjualanId").GetString().Should().Be(pToday.PenjualanId);
    }

    #endregion

    #region Private Fixtures

    private static PenjualanModel CreateSamplePenjualan(
        string penjualanId,
        string regId = "REG001",
        DateTime? trsDate = null)
    {
        var date = trsDate ?? DateTime.Today;
        var item1 = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: $"{penjualanId}001",
            brgId: "BRG01",
            brgName: "Paracetamol 500mg",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10m,
            hargaSatuan: 5000m,
            diskon: 0m,
            biaya: 1000m,
            taxProsen: 0m,
            taxRupiah: 0m,
            fee: 0m,
            etiket: "3x1",
            etiketQty: 1,
            etiketHari: 3,
            etiketKdPakai: "SESUDAH",
            etiketCatatan: "Bila demam",
            isRacik: false,
            isKomponen: false,
            racikId: "",
            isBeliBebas: false,
            nilaiKlaim: 0m);

        return PenjualanModel.CreateDirect(
            regId: regId,
            pasienName: "Budi Santoso",
            dokterId: "DOK01",
            layananId: "APT01",
            tipeJaminanId: "00000",
            kelasId: "3",
            petugasId: "PET01",
            penjualanDate: date,
            items: [item1],
            penjualanId: penjualanId);
    }

    #endregion
}
