using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class StartOpRepoTests
{
    private readonly Mock<IStartOpDal> _dalMock;
    private readonly StartOpRepo _sut;

    public StartOpRepoTests()
    {
        _dalMock = new Mock<IStartOpDal>();
        _sut = new StartOpRepo(_dalMock.Object);
    }

    [Fact]
    public void Given_NewData_When_SaveChanges_Then_ShouldCallInsert()
    {
        // Arrange
        var model = CreateDummyModel();
        var key = CreateTestKey();
        _dalMock.Setup(x => x.GetData(key))
            .Returns((StartOpDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _dalMock.Verify(x => x.Insert(It.IsAny<StartOpDto>()), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<StartOpDto>()), Times.Never);
    }

    [Fact]
    public void Given_ExistingData_When_SaveChanges_Then_ShouldCallUpdate()
    {
        // Arrange
        var model = CreateDummyModel();
        var existingDto = StartOpDto.FromModel(model);

        _dalMock.Setup(x => x.GetData(It.Is<IStartOpKey>(k => k.StartOpId == model.StartOpId)))
            .Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _dalMock.Verify(x => x.Update(It.Is<StartOpDto>(d => d.StartOpId == model.StartOpId)), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<StartOpDto>()), Times.Never);
    }

    [Fact]
    public void Given_ValidKey_When_LoadEntity_Then_ShouldReturnModel()
    {
        // Arrange
        var startOpId = "OP-123";
        var keyMock = new Mock<IStartOpKey>();
        keyMock.Setup(x => x.StartOpId).Returns(startOpId);

        var expectedDto = new StartOpDto(
            StartOpId: startOpId,
            StartOpTime: DateTime.Now,
            OrderOpId: "ORD-1",
            ScheduleOpId: "SCH-1",
            RegId: "REG-1",
            PasienId: "PAS-1",
            KamarOpId: "KM-1",
            CrtUser: "User", CrtDate: DateTime.Now,
            UpdUser: "User", UpdDate: DateTime.Now,
            VodUser: "", VodDate: DateTime.MinValue,
            NamaOperasi: "Appendectomy",
            PasienName: "John Doe",
            TglLahir: "1990-01-01",
            Gender: "L",
            KamarName: "OK-1"
        );

        // Mocking DAL menerima IStartOpKey spesifik
        _dalMock.Setup(x => x.GetData(keyMock.Object))
            .Returns(expectedDto);

        // Act
        var result = _sut.LoadEntity(keyMock.Object);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.StartOpId.Should().Be(startOpId);
    }

    [Fact]
    public void Given_Key_When_DeleteEntity_Then_ShouldCallDalDelete()
    {
        // Arrange
        var startOpId = "OP-DEL-1";
        var keyMock = new Mock<IStartOpKey>();
        keyMock.Setup(x => x.StartOpId).Returns(startOpId);

        // Act
        _sut.DeleteEntity(keyMock.Object);

        // Assert
        _dalMock.Verify(x => x.Delete(keyMock.Object), Times.Once);
    }

    private StartOpModel CreateDummyModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType("Admin", DateTime.Now),
            new AuditInfoType("Admin", DateTime.Now),
            new AuditInfoType("", DateTime.MinValue)
        );

        return new StartOpModel(
            "OP-TEST-001",
            DateTime.Now,
            audit,
            new OrderOpReff("ORD-1", DateTime.MaxValue, "Op Name"),
            new ScheduleOpReff("SCH-1", DateTime.MaxValue),
            new KamarReff("KM-1", "Kamar 1"),
            new PasienReff("P-1", "Pasien A", DateOnly.Parse("2000-01-01"), "L"),
            new RegReff("REG-1", "P-1", "Pasien A")
        );
    }

    private static IStartOpKey CreateTestKey()
        => StartOpModel.Key("A");
}