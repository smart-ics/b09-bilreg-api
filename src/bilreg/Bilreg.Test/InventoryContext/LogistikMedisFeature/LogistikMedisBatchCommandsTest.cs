using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.LogistikMedisFeature;
using Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.LogistikMedisFeature;

public class LogistikMedisBatchCommandsTest
{
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();
    private readonly Mock<IMutasiBrgMovementService> _movementServiceMock = new();
    private readonly Mock<ISqlDatabase> _sqlDatabaseMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();
    private readonly Mock<ILogistikMedisBffDal> _bffDalMock = new();

    public LogistikMedisBatchCommandsTest()
    {
        _sqlDatabaseMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()))
            .Callback<Action<SqlConnection, SqlTransaction>>(action => action(null!, null!));

        SetupLayanan("LY01", "Poli Anak");
        SetupLayanan("GD01", "Gudang Farmasi");
        SetupSatuan("TAB", "Tablet");
        SetupSatuan("PCS", "Pieces");
    }

    private LogistikMedisOrderMutasiSubmitBatchHandler CreateSubmitBatchHandler() =>
        new(
            _orderMutasiRepoMock.Object,
            _layananRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _bffDalMock.Object);

    private LogistikMedisMutasiKeluarDispatchBatchHandler CreateDispatchBatchHandler() =>
        new(
            _mutasiBrgRepoMock.Object,
            _movementServiceMock.Object,
            _sqlDatabaseMock.Object,
            _layananRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _bffDalMock.Object,
            _orderMutasiRepoMock.Object);

    private void SetupLayanan(string id, string name)
    {
        var layanan = LayananType.Default with { LayananId = id, LayananName = name };
        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == id)))
            .Returns(MayBe.From(layanan));
    }

    private void SetupSatuan(string id, string name)
    {
        var satuan = new SatuanType(id, name);
        _satuanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == id)))
            .Returns(MayBe.From(satuan));
    }

    private void SetupBrg(string id, string name, bool isAktif, string groupRekDkId)
    {
        var mock = new Mock<IBrg>();
        mock.Setup(x => x.BrgId).Returns(id);
        mock.Setup(x => x.BrgName).Returns(name);
        mock.Setup(x => x.IsAktif).Returns(isAktif);
        mock.Setup(x => x.GroupRekDk).Returns(new GroupRekDkType(groupRekDkId, groupRekDkId));
        mock.Setup(x => x.ToReff()).Returns(new BrgReff(id, name));

        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == id)))
            .Returns(MayBe.From(mock.Object));
    }

    #region SubmitBatch OrderMutasi

    [Fact]
    public async Task SubmitBatch_Success_CreatesAndSubmitsOrderWithActualStokCurrent()
    {
        SetupBrg("BRG01", "Paracetamol", true, "OBT");
        SetupBrg("BRG02", "Spuit 3cc", true, "BHP");

        _bffDalMock
            .Setup(x => x.ListStokBatch("LY01"))
            .Returns(
            [
                new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol", "TAB", "Tablet", "OBT", "B1", "2026-12-31", 100m, 12m),
                new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol", "TAB", "Tablet", "OBT", "B2", "2027-06-30", 100m, 8m),
            ]);

        OrderMutasiModel? savedOrder = null;
        _orderMutasiRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()))
            .Callback<OrderMutasiModel>(o => savedOrder = o);

        var sut = CreateSubmitBatchHandler();
        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            LayananOrderId: "LY01",
            LayananTujuanId: "GD01",
            UserId: "USER01",
            Priority: "Cito",
            OrderNote: "Stok menipis",
            ListItem:
            [
                new LogistikMedisOrderMutasiSubmitItemRequest("BRG01", 30m, "TAB"),
                new LogistikMedisOrderMutasiSubmitItemRequest("BRG02", 50m, "PCS"),
            ]);

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.OrderMutasiId.Should().NotBeNullOrWhiteSpace();
        savedOrder.Should().NotBeNull();
        savedOrder!.OrderMutasiId.Should().Be(response.OrderMutasiId);
        savedOrder.State.Should().Be(OrderMutasiStateEnum.Submitted);
        savedOrder.Priority.Should().Be(OrderMutasiPriorityEnum.Cito);
        savedOrder.LayananOrder.LayananId.Should().Be("LY01");
        savedOrder.LayananTujuan.LayananId.Should().Be("GD01");
        savedOrder.OrderNote.Should().Be("Stok menipis");
        savedOrder.ListItem.Should().HaveCount(2);
        savedOrder.ListItem.First(x => x.Brg.BrgId == "BRG01").StokCurrent.Should().Be(20m);
        savedOrder.ListItem.First(x => x.Brg.BrgId == "BRG02").StokCurrent.Should().Be(0m);
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()), Times.Once);
    }

    [Fact]
    public async Task SubmitBatch_SameLayananOrderAndTujuan_ThrowsInvalidOperationException()
    {
        var sut = CreateSubmitBatchHandler();
        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            "LY01",
            "LY01",
            "USER01",
            "Rutin",
            "-",
            [new LogistikMedisOrderMutasiSubmitItemRequest("BRG01", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _orderMutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()), Times.Never);
    }

    [Fact]
    public async Task SubmitBatch_EmptyListItem_ThrowsArgumentException()
    {
        var sut = CreateSubmitBatchHandler();
        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            "LY01",
            "GD01",
            "USER01",
            "Rutin",
            "-",
            []);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SubmitBatch_InvalidPriority_ThrowsArgumentException()
    {
        var sut = CreateSubmitBatchHandler();
        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            "LY01",
            "GD01",
            "USER01",
            "URGENT",
            "-",
            [new LogistikMedisOrderMutasiSubmitItemRequest("BRG01", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SubmitBatch_InactiveBrg_ThrowsInvalidOperationException()
    {
        SetupBrg("BRG-NON", "Obat Non Aktif", false, "OBT");
        var sut = CreateSubmitBatchHandler();
        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            "LY01",
            "GD01",
            "USER01",
            "Rutin",
            "-",
            [new LogistikMedisOrderMutasiSubmitItemRequest("BRG-NON", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region DispatchBatch MutasiKeluar

    [Fact]
    public async Task DispatchBatch_SuccessWithFefoResolution_DispatchesDeductsStockAndSavesInTransaction()
    {
        SetupBrg("BRG01", "Paracetamol", true, "OBT");
        SetupBrg("ATK01", "Kertas Resep", true, "ATK");

        _bffDalMock
            .Setup(x => x.ListStokBatch("GD01"))
            .Returns(
            [
                new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol", "TAB", "Tablet", "OBT", "FEFO-01", "2026-10-15", 100m, 25m),
                new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol", "TAB", "Tablet", "OBT", "FEFO-02", "2027-01-20", 100m, 40m),
            ]);

        MutasiBrgModel? savedMutasi = null;
        _mutasiBrgRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<MutasiBrgModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()))
            .Callback<MutasiBrgModel, IDbConnection, IDbTransaction>((m, _, _) => savedMutasi = m);

        var sut = CreateDispatchBatchHandler();
        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            LayananAsalId: "GD01",
            LayananTujuanId: "LY01",
            UserId: "USER01",
            DispatchNote: "Kirim rutin",
            OrderMutasiId: "",
            ListItem:
            [
                new LogistikMedisMutasiKeluarDispatchItemRequest("BRG01", "", "", 10m, "TAB"),
                new LogistikMedisMutasiKeluarDispatchItemRequest("ATK01", "", "", 5m, "PCS"),
            ]);

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.MutasiBrgId.Should().NotBeNullOrWhiteSpace();
        savedMutasi.Should().NotBeNull();
        savedMutasi!.MutasiBrgId.Should().Be(response.MutasiBrgId);
        savedMutasi.State.Should().Be(MutasiBrgStateEnum.Dispatched);
        savedMutasi.IssuerUserId.Should().Be("USER01");
        savedMutasi.ListItem.Should().HaveCount(2);
        savedMutasi.ListItem[0].BatchNo.Should().Be("FEFO-01");
        savedMutasi.ListItem[0].ExpiredDate.Should().Be("2026-10-15");

        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Once);
        _movementServiceMock.Verify(x => x.DeductStock(savedMutasi, It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()), Times.Once);
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(savedMutasi, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
    }

    [Fact]
    public async Task DispatchBatch_SameLayananAsalAndTujuan_ThrowsArgumentException()
    {
        var sut = CreateDispatchBatchHandler();
        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            "GD01",
            "GD01",
            "USER01",
            "Note",
            "",
            [new LogistikMedisMutasiKeluarDispatchItemRequest("BRG01", "B1", "2026-12-31", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Never);
    }

    [Fact]
    public async Task DispatchBatch_PharmacyWithoutExpiredDateAndNoFefoStock_ThrowsArgumentException()
    {
        SetupBrg("BRG01", "Paracetamol", true, "OBT");
        _bffDalMock.Setup(x => x.ListStokBatch("GD01")).Returns([]);

        var sut = CreateDispatchBatchHandler();
        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            "GD01",
            "LY01",
            "USER01",
            "Note",
            "",
            [new LogistikMedisMutasiKeluarDispatchItemRequest("BRG01", "", "", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Never);
    }

    [Fact]
    public async Task DispatchBatch_EmptyListItem_ThrowsArgumentException()
    {
        var sut = CreateDispatchBatchHandler();
        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            "GD01",
            "LY01",
            "USER01",
            "Note",
            "",
            []);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DispatchBatch_InactiveBrg_ThrowsInvalidOperationException()
    {
        SetupBrg("BRG-NON", "Obat Non Aktif", false, "OBT");
        var sut = CreateDispatchBatchHandler();
        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            "GD01",
            "LY01",
            "USER01",
            "Note",
            "",
            [new LogistikMedisMutasiKeluarDispatchItemRequest("BRG-NON", "B1", "2026-12-31", 5m, "TAB")]);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Never);
    }

    [Fact]
    public async Task SubmitBatch_WithCustomBusinessDate_UsesTglJamProviderNow()
    {
        SetupBrg("BRG01", "Paracetamol", true, "OBT");
        _bffDalMock.Setup(x => x.ListStokBatch("LY01")).Returns([]);

        var businessNow = new DateTime(2024, 6, 6, 10, 30, 0);
        var tglJamMock = new Mock<ITglJamProvider>();
        tglJamMock.Setup(x => x.Now).Returns(businessNow);

        OrderMutasiModel? savedOrder = null;
        _orderMutasiRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<OrderMutasiModel>()))
            .Callback<OrderMutasiModel>(o => savedOrder = o);

        var sut = new LogistikMedisOrderMutasiSubmitBatchHandler(
            _orderMutasiRepoMock.Object,
            _layananRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _bffDalMock.Object,
            tglJamMock.Object);

        var cmd = new LogistikMedisOrderMutasiSubmitBatchCommand(
            LayananOrderId: "LY01",
            LayananTujuanId: "GD01",
            UserId: "USER01",
            Priority: "Rutin",
            OrderNote: "Request bisnis date",
            ListItem: [new LogistikMedisOrderMutasiSubmitItemRequest("BRG01", 10m, "TAB")]);

        await sut.Handle(cmd, CancellationToken.None);

        savedOrder.Should().NotBeNull();
        savedOrder!.OrderMutasiDate.Should().Be(businessNow);
        savedOrder.AuditTrail.Created.Timestamp.Should().Be(businessNow);
        savedOrder.AuditTrail.Modified.Timestamp.Should().Be(businessNow);
    }

    [Fact]
    public async Task DispatchBatch_WithCustomBusinessDate_UsesTglJamProviderNow()
    {
        SetupBrg("BRG01", "Paracetamol", true, "OBT");

        var businessNow = new DateTime(2024, 6, 6, 11, 15, 0);
        var tglJamMock = new Mock<ITglJamProvider>();
        tglJamMock.Setup(x => x.Now).Returns(businessNow);

        MutasiBrgModel? savedMutasi = null;
        _mutasiBrgRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<MutasiBrgModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()))
            .Callback<MutasiBrgModel, IDbConnection, IDbTransaction>((m, _, _) => savedMutasi = m);

        var sut = new LogistikMedisMutasiKeluarDispatchBatchHandler(
            _mutasiBrgRepoMock.Object,
            _movementServiceMock.Object,
            _sqlDatabaseMock.Object,
            _layananRepoMock.Object,
            _brgRepoMock.Object,
            _satuanRepoMock.Object,
            _bffDalMock.Object,
            _orderMutasiRepoMock.Object,
            tglJamMock.Object);

        var cmd = new LogistikMedisMutasiKeluarDispatchBatchCommand(
            LayananAsalId: "GD01",
            LayananTujuanId: "LY01",
            UserId: "USER01",
            DispatchNote: "Kirim bisnis date",
            OrderMutasiId: "",
            ListItem: [new LogistikMedisMutasiKeluarDispatchItemRequest("BRG01", "B1", "2026-12-31", 5m, "TAB")]);

        await sut.Handle(cmd, CancellationToken.None);

        savedMutasi.Should().NotBeNull();
        savedMutasi!.MutasiBrgDate.Should().Be(businessNow);
        savedMutasi.AuditTrail.Created.Timestamp.Should().Be(businessNow);
        savedMutasi.AuditTrail.Modified.Timestamp.Should().Be(businessNow);
    }

    #endregion
}
