using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class TerimaMutasiConfirmHandlerTest
{
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();
    private readonly Mock<ITerimaMutasiRepo> _terimaMutasiRepoMock = new();
    private readonly Mock<ITerimaMutasiMovementService> _movementServiceMock = new();
    private readonly Mock<ISqlDatabase> _sqlDatabaseMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    public TerimaMutasiConfirmHandlerTest()
    {
        _sqlDatabaseMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()))
            .Callback<Action<SqlConnection, SqlTransaction>>(action => action(null!, null!));
    }

    private TerimaMutasiConfirmHandler CreateSut() =>
        new(
            _mutasiBrgRepoMock.Object,
            _terimaMutasiRepoMock.Object,
            _movementServiceMock.Object,
            _sqlDatabaseMock.Object,
            _auditRepoMock.Object);

    private static MutasiBrgModel CreateDispatchedMutasi(
        string id = "MT2600001A",
        string layananAsalId = "L01",
        string layananTujuanId = "L02",
        string orderMutasiId = "OM2600001A")
    {
        var mutasi = MutasiBrgModel.Create(
            id,
            new DateTime(2026, 11, 26, 8, 0, 0),
            new LayananReff(layananAsalId, "Gudang Farmasi"),
            new LayananReff(layananTujuanId, "Depo Rawat Inap"),
            orderMutasiId: orderMutasiId,
            dispatchNote: "Kirim pagi",
            userId: "USR_ASAL");

        mutasi.AddItem(MutasiBrgItemModel.Create(
            1,
            new BrgReff("B001", "Paracetamol 500mg"),
            batchNo: "BATCH-01",
            expiredDate: "2027-12-31",
            qtyKirim: 12m,
            satuan: new SatuanType("STR", "Strip")));

        mutasi.AddItem(MutasiBrgItemModel.Create(
            2,
            new BrgReff("B002", "Amoxicillin 500mg"),
            batchNo: "BATCH-02",
            expiredDate: "2027-06-30",
            qtyKirim: 5m,
            satuan: new SatuanType("BOX", "Box")));

        mutasi.Dispatch("USR_ASAL", new DateTime(2026, 11, 26, 9, 0, 0));
        return mutasi;
    }

    [Fact]
    public async Task Handle_ValidDispatchedMutasi_ShouldConfirmFullReceipt_SettleStock_UpdateState_KeepQtyKirim_AndAudit()
    {
        // Arrange
        var mutasi = CreateDispatchedMutasi("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntityForUpdate(
                It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A"),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .Returns(MayBe.From(mutasi));

        TerimaMutasiModel? savedTerima = null;
        _terimaMutasiRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TerimaMutasiModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()))
            .Callback<TerimaMutasiModel, IDbConnection, IDbTransaction>((m, _, _) => savedTerima = m);

        MutasiBrgModel? savedMutasi = null;
        _mutasiBrgRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<MutasiBrgModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()))
            .Callback<MutasiBrgModel, IDbConnection, IDbTransaction>((m, _, _) => savedMutasi = m);

        var sut = CreateSut();
        var command = new TerimaMutasiConfirmCommand("MT2600001A", " l02 ", "USR_TERIMA", "Diterima lengkap");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TerimaMutasiId.Should().StartWith("TM");
        result.State.Should().Be(nameof(TerimaMutasiStateEnum.Received));

        savedTerima.Should().NotBeNull();
        savedTerima!.TerimaMutasiId.Should().Be(result.TerimaMutasiId);
        savedTerima.MutasiBrgId.Should().Be("MT2600001A");
        savedTerima.OrderMutasiId.Should().Be("OM2600001A");
        savedTerima.State.Should().Be(TerimaMutasiStateEnum.Received);
        savedTerima.ReceiverUserId.Should().Be("USR_TERIMA");
        savedTerima.Note.Should().Be("Diterima lengkap");
        savedTerima.ListItem.Should().HaveCount(2);
        savedTerima.ListItem[0].Brg.BrgId.Should().Be("B001");
        savedTerima.ListItem[0].QtyTerima.Should().Be(12m);
        savedTerima.ListItem[1].Brg.BrgId.Should().Be("B002");
        savedTerima.ListItem[1].QtyTerima.Should().Be(5m);

        savedMutasi.Should().NotBeNull();
        savedMutasi!.State.Should().Be(MutasiBrgStateEnum.Received);
        savedMutasi.ListItem[0].QtyKirim.Should().Be(12m);
        savedMutasi.ListItem[1].QtyKirim.Should().Be(5m);

        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Once);
        _movementServiceMock.Verify(
            x => x.SettleReceivedStock(It.Is<TerimaMutasiModel>(t => t.TerimaMutasiId == result.TerimaMutasiId), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()),
            Times.Once);
        _terimaMutasiRepoMock.Verify(
            x => x.SaveChanges(It.IsAny<TerimaMutasiModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()),
            Times.Once);
        _mutasiBrgRepoMock.Verify(
            x => x.SaveChanges(mutasi, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()),
            Times.Once);
        _auditRepoMock.Verify(
            x => x.SaveChanges(It.Is<AuditLog>(a =>
                a.ActionType == "CONFIRM_TERIMA_MUTASI" &&
                a.EntityName == nameof(TerimaMutasiModel) &&
                a.EntityId == result.TerimaMutasiId &&
                a.CorrelationId == "MT2600001A" &&
                a.Reason == "Diterima lengkap")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMutasiNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntityForUpdate(
                It.IsAny<IMutasiBrgKey>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .Returns(MayBe<MutasiBrgModel>.None);

        var sut = CreateSut();
        var command = new TerimaMutasiConfirmCommand("MT_MISSING", "L02", "USR_TERIMA");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MT_MISSING*");
        _movementServiceMock.Verify(
            x => x.SettleReceivedStock(It.IsAny<TerimaMutasiModel>(), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()),
            Times.Never);
        _terimaMutasiRepoMock.Verify(
            x => x.SaveChanges(It.IsAny<TerimaMutasiModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()),
            Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData(MutasiBrgStateEnum.Draft)]
    [InlineData(MutasiBrgStateEnum.Cancelled)]
    [InlineData(MutasiBrgStateEnum.Received)]
    public async Task Handle_WhenMutasiStateIsNotDispatched_ShouldThrowInvalidOperationException(MutasiBrgStateEnum state)
    {
        // Arrange
        var mutasi = CreateDispatchedMutasi("MT2600001A");
        switch (state)
        {
            case MutasiBrgStateEnum.Draft:
                mutasi = MutasiBrgModel.Create(
                    "MT2600001A",
                    DateTime.Now,
                    new LayananReff("L01", "Gudang Farmasi"),
                    new LayananReff("L02", "Depo Rawat Inap"),
                    userId: "USR_ASAL");
                break;
            case MutasiBrgStateEnum.Cancelled:
                mutasi.Cancel("Salah kirim", "USR_ASAL");
                break;
            case MutasiBrgStateEnum.Received:
                mutasi.MarkReceived("USR_LAIN");
                break;
        }

        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntityForUpdate(
                It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A"),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new TerimaMutasiConfirmCommand("MT2600001A", "L02", "USR_TERIMA");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dispatched*");
        _movementServiceMock.Verify(
            x => x.SettleReceivedStock(It.IsAny<TerimaMutasiModel>(), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()),
            Times.Never);
        _terimaMutasiRepoMock.Verify(
            x => x.SaveChanges(It.IsAny<TerimaMutasiModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()),
            Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLayananPenerimaDoesNotMatchLayananTujuan_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mutasi = CreateDispatchedMutasi("MT2600001A", layananTujuanId: "L02");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntityForUpdate(
                It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A"),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .Returns(MayBe.From(mutasi));

        var sut = CreateSut();
        var command = new TerimaMutasiConfirmCommand("MT2600001A", "L99", "USR_TERIMA");

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unit penerima*L99*L02*");
        _movementServiceMock.Verify(
            x => x.SettleReceivedStock(It.IsAny<TerimaMutasiModel>(), It.IsAny<SqlConnection>(), It.IsAny<SqlTransaction>()),
            Times.Never);
        _terimaMutasiRepoMock.Verify(
            x => x.SaveChanges(It.IsAny<TerimaMutasiModel>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()),
            Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData("", "L02", "USR_TERIMA")]
    [InlineData("MT2600001A", "", "USR_TERIMA")]
    [InlineData("MT2600001A", "L02", "")]
    public async Task Handle_WhenRequiredInputMissing_ShouldThrowArgumentException(
        string mutasiBrgId,
        string layananPenerimaId,
        string userId)
    {
        // Arrange
        var sut = CreateSut();
        var command = new TerimaMutasiConfirmCommand(mutasiBrgId, layananPenerimaId, userId);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _sqlDatabaseMock.Verify(x => x.ExecuteTransaction(It.IsAny<Action<SqlConnection, SqlTransaction>>()), Times.Never);
    }
}
