using Bilreg.Api.Controllers.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class PakaiBrgQueriesTests
{
    private static PakaiBrgModel CreateSampleModel(
        string id = "PK26100001",
        DateTime? tglPakai = null,
        bool isVoid = false)
    {
        var layanan = new LayananReff("LY01", "Poli Umum");
        var item1 = PakaiBrgItemModel.Create(
            noUrut: 1,
            brg: new BrgReff("BRG01", "Spuit 3cc"),
            satuan: new SatuanReff("PCS", "Pieces"),
            stokCurrent: 50,
            qty: 5,
            noBatch: "BATCH-A1",
            tglEd: new DateTime(2027, 6, 1),
            hpp: 1500,
            keterangan: "Injeksi poli"
        );
        var item2 = PakaiBrgItemModel.Create(
            noUrut: 2,
            brg: new BrgReff("BRG02", "Alkohol 70%"),
            satuan: new SatuanReff("BTL", "Botol"),
            stokCurrent: 20,
            qty: 2,
            noBatch: "BATCH-B2",
            tglEd: new DateTime(2028, 1, 15),
            hpp: 25000,
            keterangan: "Disinfeksi"
        );

        var model = PakaiBrgModel.Create(
            tglPakai: tglPakai ?? new DateTime(2026, 10, 23, 10, 15, 30),
            layanan: layanan,
            petugasId: "USR01",
            keterangan: "Pemakaian harian poli",
            listItem: [item1, item2],
            pakaiBrgId: id
        );

        if (isVoid)
        {
            model.Void("USR_VOID", "Salah input kuantitas", new DateTime(2026, 10, 23, 14, 0, 0));
        }

        return model;
    }

    #region GET QUERY TESTS
    [Fact]
    public async Task GetQuery_GivenValidPakaiBrgId_ReturnsFullDocumentDetail()
    {
        // ARRANGE
        var repoMock = new Mock<IPakaiBrgRepo>();
        var sample = CreateSampleModel();
        repoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == sample.PakaiBrgId)))
            .Returns(MayBe.From(sample));

        var handler = new PakaiBrgGetHandler(repoMock.Object);
        var query = new PakaiBrgGetQuery(sample.PakaiBrgId);

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.PakaiBrgId.Should().Be(sample.PakaiBrgId);
        result.TglPakai.Should().Be("2026-10-23");
        result.JamPakai.Should().Be("10:15:30");
        result.LayananId.Should().Be("LY01");
        result.LayananName.Should().Be("Poli Umum");
        result.PetugasId.Should().Be("USR01");
        result.Keterangan.Should().Be("Pemakaian harian poli");
        result.IsVoid.Should().BeFalse();
        result.TglVoid.Should().BeEmpty();
        result.PetugasVoidId.Should().BeEmpty();
        result.AlasanVoid.Should().BeEmpty();
        result.TotalNilai.Should().Be(5 * 1500 + 2 * 25000); // 7500 + 50000 = 57500

        result.ListItem.Should().HaveCount(2);
        var item1 = result.ListItem[0];
        item1.NoUrut.Should().Be(1);
        item1.BrgId.Should().Be("BRG01");
        item1.BrgName.Should().Be("Spuit 3cc");
        item1.SatuanId.Should().Be("PCS");
        item1.Qty.Should().Be(5);
        item1.NoBatch.Should().Be("BATCH-A1");
        item1.TglEd.Should().Be("2027-06-01");
        item1.Hpp.Should().Be(1500);
        item1.SubTotal.Should().Be(7500);
    }

    [Fact]
    public async Task GetQuery_GivenVoidedDocument_ReturnsVoidAuditDetails()
    {
        // ARRANGE
        var repoMock = new Mock<IPakaiBrgRepo>();
        var sample = CreateSampleModel(isVoid: true);
        repoMock.Setup(x => x.LoadEntity(It.IsAny<IPakaiBrgKey>()))
            .Returns(MayBe.From(sample));

        var handler = new PakaiBrgGetHandler(repoMock.Object);
        var query = new PakaiBrgGetQuery(sample.PakaiBrgId);

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.IsVoid.Should().BeTrue();
        result.TglVoid.Should().Be("2026-10-23 14:00:00");
        result.PetugasVoidId.Should().Be("USR_VOID");
        result.AlasanVoid.Should().Be("Salah input kuantitas");
    }

    [Fact]
    public async Task GetQuery_GivenDocumentNotFound_ThrowsKeyNotFoundException()
    {
        // ARRANGE
        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.LoadEntity(It.IsAny<IPakaiBrgKey>()))
            .Returns(MayBe<PakaiBrgModel>.None);

        var handler = new PakaiBrgGetHandler(repoMock.Object);
        var query = new PakaiBrgGetQuery("PK_NONEXIST");

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PK_NONEXIST*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetQuery_GivenInvalidId_ThrowsArgumentException(string? invalidId)
    {
        var repoMock = new Mock<IPakaiBrgRepo>();
        var handler = new PakaiBrgGetHandler(repoMock.Object);
        var query = new PakaiBrgGetQuery(invalidId!);

        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region LIST BY LAYANAN QUERY TESTS
    [Fact]
    public async Task ListByLayananQuery_GivenValidParams_ReturnsSortedNewestFirst()
    {
        // ARRANGE
        var doc1 = CreateSampleModel("PK26100001", new DateTime(2026, 10, 20, 8, 0, 0));
        var doc2 = CreateSampleModel("PK26100002", new DateTime(2026, 10, 23, 14, 30, 0));
        var doc3 = CreateSampleModel("PK26100003", new DateTime(2026, 10, 22, 11, 0, 0));

        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.ListByLayanan(
                It.Is<ILayananKey>(l => l.LayananId == "LY01"),
                It.IsAny<Periode>()))
            .Returns([doc1, doc2, doc3]);

        var handler = new PakaiBrgListByLayananHandler(repoMock.Object);
        var query = new PakaiBrgListByLayananQuery("LY01", "2026-10-20", "2026-10-23");

        // ACT
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(3);
        // doc2 (Oct 23) -> doc3 (Oct 22) -> doc1 (Oct 20)
        result[0].PakaiBrgId.Should().Be("PK26100002");
        result[1].PakaiBrgId.Should().Be("PK26100003");
        result[2].PakaiBrgId.Should().Be("PK26100001");
        result[0].ListItem.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("", "2026-10-01", "2026-10-31")]
    [InlineData("LY01", "invalid-date", "2026-10-31")]
    [InlineData("LY01", "2026-10-01", "invalid-date")]
    public async Task ListByLayananQuery_GivenInvalidParams_ThrowsArgumentException(
        string layananId, string tgl1, string tgl2)
    {
        var repoMock = new Mock<IPakaiBrgRepo>();
        var handler = new PakaiBrgListByLayananHandler(repoMock.Object);
        var query = new PakaiBrgListByLayananQuery(layananId, tgl1, tgl2);

        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListByLayananQuery_GivenNoRecords_ReturnsEmptyList()
    {
        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.ListByLayanan(It.IsAny<ILayananKey>(), It.IsAny<Periode>()))
            .Returns([]);

        var handler = new PakaiBrgListByLayananHandler(repoMock.Object);
        var query = new PakaiBrgListByLayananQuery("LY01", "2026-10-01", "2026-10-31");

        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }
    #endregion

    #region LIST STOK LOKASI QUERY TESTS
    [Fact]
    public async Task ListStokLokasiQuery_GivenValidLayanan_ReturnsStockItemsWithBatchesFEFO()
    {
        // ARRANGE
        var batch1 = new PakaiBrgStokBatchView("B02", new DateTime(2027, 5, 1), 10, 1000);
        var batch2 = new PakaiBrgStokBatchView("B01", new DateTime(2026, 12, 1), 5, 950);

        var item1 = new PakaiBrgStokLokasiView(
            BrgId: "BRG01",
            BrgName: "Paracetamol 500mg",
            TotalStok: 15,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            GroupRekDkId: "OBT",
            HppDefault: 950,
            Batches: [batch1, batch2]); // Deliberately unsorted in raw

        var item2 = new PakaiBrgStokLokasiView(
            BrgId: "BRG02",
            BrgName: "Kasa Steril",
            TotalStok: 25,
            SatuanId: "KOT",
            SatuanName: "Kotak",
            GroupRekDkId: "BHP",
            HppDefault: 12000,
            Batches: [new PakaiBrgStokBatchView("B10", new DateTime(2028, 1, 1), 25, 12000)]);

        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.ListStokLokasi(It.Is<ILayananKey>(l => l.LayananId == "LY01")))
            .Returns([item1, item2]);

        var handler = new PakaiBrgListStokLokasiHandler(repoMock.Object);
        var query = new PakaiBrgListStokLokasiQuery("LY01");

        // ACT
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(2);
        var res1 = result[0];
        res1.BrgId.Should().Be("BRG01");
        res1.TotalStok.Should().Be(15);
        res1.GroupRekDkId.Should().Be("OBT");
        res1.HppDefault.Should().Be(950);
        res1.Batches.Should().HaveCount(2);
        // Verified sorted FEFO: B01 (Dec 2026) before B02 (May 2027)
        res1.Batches[0].NoBatch.Should().Be("B01");
        res1.Batches[0].TglEd.Should().Be("2026-12-01");
        res1.Batches[1].NoBatch.Should().Be("B02");
        res1.Batches[1].TglEd.Should().Be("2027-05-01");
    }

    [Fact]
    public async Task ListStokLokasiQuery_GivenKeyword_FiltersByBrgNameOrBrgId()
    {
        // ARRANGE
        var item1 = new PakaiBrgStokLokasiView(
            BrgId: "BRG01",
            BrgName: "Paracetamol 500mg",
            TotalStok: 10,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            GroupRekDkId: "OBT",
            HppDefault: 950,
            Batches: [new PakaiBrgStokBatchView("B01", new DateTime(2027, 1, 1), 10, 950)]);

        var item2 = new PakaiBrgStokLokasiView(
            BrgId: "BRG02",
            BrgName: "Amoxicillin 500mg",
            TotalStok: 20,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            GroupRekDkId: "OBT",
            HppDefault: 1200,
            Batches: [new PakaiBrgStokBatchView("B02", new DateTime(2027, 2, 1), 20, 1200)]);

        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.ListStokLokasi(It.IsAny<ILayananKey>()))
            .Returns([item1, item2]);

        var handler = new PakaiBrgListStokLokasiHandler(repoMock.Object);

        // ACT: Search by partial name "amox"
        var query = new PakaiBrgListStokLokasiQuery("LY01", "amox");
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        result[0].BrgId.Should().Be("BRG02");
        result[0].BrgName.Should().Be("Amoxicillin 500mg");

        // ACT 2: Search by code "BRG01"
        var query2 = new PakaiBrgListStokLokasiQuery("LY01", "BRG01");
        var result2 = (await handler.Handle(query2, CancellationToken.None)).ToList();
        result2.Should().HaveCount(1);
        result2[0].BrgId.Should().Be("BRG01");
    }

    [Fact]
    public async Task ListStokLokasiQuery_ExcludesZeroStockItemsAndBatches()
    {
        // ARRANGE: item with 0 total stock, and item with a 0-qty batch
        var itemZero = new PakaiBrgStokLokasiView(
            BrgId: "BRG00",
            BrgName: "Habis Total",
            TotalStok: 0,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            GroupRekDkId: "OBT",
            HppDefault: 1000,
            Batches: []);

        var itemWithZeroBatch = new PakaiBrgStokLokasiView(
            BrgId: "BRG01",
            BrgName: "Ada Stok",
            TotalStok: 10,
            SatuanId: "TAB",
            SatuanName: "Tablet",
            GroupRekDkId: "OBT",
            HppDefault: 1000,
            Batches: [
                new PakaiBrgStokBatchView("B01", new DateTime(2027, 1, 1), 0, 1000),
                new PakaiBrgStokBatchView("B02", new DateTime(2027, 2, 1), 10, 1000)
            ]);

        var repoMock = new Mock<IPakaiBrgRepo>();
        repoMock.Setup(x => x.ListStokLokasi(It.IsAny<ILayananKey>()))
            .Returns([itemZero, itemWithZeroBatch]);

        var handler = new PakaiBrgListStokLokasiHandler(repoMock.Object);
        var query = new PakaiBrgListStokLokasiQuery("LY01");

        // ACT
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        result[0].BrgId.Should().Be("BRG01");
        result[0].Batches.Should().HaveCount(1);
        result[0].Batches[0].NoBatch.Should().Be("B02");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ListStokLokasiQuery_GivenInvalidLayanan_ThrowsArgumentException(string? invalidLayanan)
    {
        var repoMock = new Mock<IPakaiBrgRepo>();
        var handler = new PakaiBrgListStokLokasiHandler(repoMock.Object);
        var query = new PakaiBrgListStokLokasiQuery(invalidLayanan!);

        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region CONTROLLER TESTS
    [Fact]
    public async Task Controller_Create_SendsCommandAndReturnsOkJSend()
    {
        // ARRANGE
        var mediatorMock = new Mock<IMediator>();
        var cmd = new PakaiBrgCreateCommand(
            LayananId: "LY01",
            UserId: "USR01",
            Keterangan: "Pakai kapas",
            ListItem: [new PakaiBrgCreateItemRequest("BRG01", 2)]
        );
        var expectedResponse = new PakaiBrgCreateResponse("PK26100001", DateTime.Now, 3000, 1);
        mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new PakaiBrgController(mediatorMock.Object);

        // ACT
        var actionResult = await controller.Create(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Controller_Void_SendsCommandAndReturnsOkJSend()
    {
        // ARRANGE
        var mediatorMock = new Mock<IMediator>();
        var cmd = new PakaiBrgVoidCommand("PK26100001", "USR01", "Salah input");
        var expectedResponse = new PakaiBrgVoidResponse("PK26100001", true, DateTime.Now, "Salah input");
        mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new PakaiBrgController(mediatorMock.Object);

        // ACT
        var actionResult = await controller.Void(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Controller_Get_SendsQueryAndReturnsOkJSend()
    {
        // ARRANGE
        var mediatorMock = new Mock<IMediator>();
        var expectedResponse = new PakaiBrgGetResponse(
            "PK26100001", "2026-10-23", "10:00:00", "LY01", "Poli Umum",
            "USR01", "Catatan", false, "", "", "", 5000, []
        );
        mediatorMock.Setup(x => x.Send(It.Is<PakaiBrgGetQuery>(q => q.PakaiBrgId == "PK26100001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new PakaiBrgController(mediatorMock.Object);

        // ACT
        var actionResult = await controller.Get("PK26100001");

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Controller_ListByLayanan_SendsQueryAndReturnsOkJSend()
    {
        // ARRANGE
        var mediatorMock = new Mock<IMediator>();
        var listResponse = new List<PakaiBrgListByLayananResponse>
        {
            new("PK26100001", "2026-10-23", "10:00:00", "LY01", "Poli Umum", "USR01", "Catatan", false, "", "", "", 5000, [])
        };
        mediatorMock.Setup(x => x.Send(
                It.Is<PakaiBrgListByLayananQuery>(q => q.LayananId == "LY01" && q.TglYmd1 == "2026-10-01" && q.TglYmd2 == "2026-10-23"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        var controller = new PakaiBrgController(mediatorMock.Object);

        // ACT
        var actionResult = await controller.ListByLayanan("LY01", "2026-10-01", "2026-10-23");

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(listResponse);
    }

    [Fact]
    public async Task Controller_ListStokLokasi_SendsQueryAndReturnsOkJSend()
    {
        // ARRANGE
        var mediatorMock = new Mock<IMediator>();
        var listResponse = new List<PakaiBrgListStokLokasiResponse>
        {
            new("BRG01", "Spuit", 10, "PCS", "Pieces", "BHP", 1500, [])
        };
        mediatorMock.Setup(x => x.Send(
                It.Is<PakaiBrgListStokLokasiQuery>(q => q.LayananId == "LY01" && q.Keyword == "spuit"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        var controller = new PakaiBrgController(mediatorMock.Object);

        // ACT
        var actionResult = await controller.ListStokLokasi("LY01", "spuit");

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(listResponse);
    }
    #endregion
}
