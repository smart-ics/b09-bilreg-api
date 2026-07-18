using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class WaitingListRepoTest
{
    private readonly Mock<IWaitingListDal> _dalMock = new();
    private readonly WaitingListRepo _repository;

    public WaitingListRepoTest() => _repository = new WaitingListRepo(_dalMock.Object);

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IWaitingListKey>())).Returns((WaitingListDto)null!);

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.IsAny<WaitingListDto>()), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<WaitingListDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IWaitingListKey>())).Returns(CreateTestDto());

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.IsAny<WaitingListDto>()), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<WaitingListDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto();
        var key = WaitingListModel.Key("WTL00000001");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.WaitingListId.Should().Be("WTL00000001");
                model.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
                model.RegId.Should().Be("RG00001234");
                model.Pasien.PasienId.Should().Be("P0001");
                model.Pasien.PasienName.Should().Be("Pasien Test");
                model.Pasien.TglLahir.Should().Be(new DateOnly(1990, 5, 15));
                model.Pasien.Gender.Should().Be("L");
                model.KelasRawat.KelasId.Should().Be("K01");
                model.Bangsal.BangsalId.Should().Be("B001");
                model.Priority.Should().Be(5);
                model.IsActive.Should().BeTrue();
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT_RP_04_GivenRegId_WhenHasActiveByRegId_ThenDelegatesToDal()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00001234")).Returns(CreateTestDto());

        var result = _repository.HasActiveByRegId("RG00001234");

        result.Should().BeTrue();
        _dalMock.Verify(x => x.GetActiveByRegId("RG00001234"), Times.Once);
    }

    [Fact]
    public void UT_RP_05_GivenActiveWaitingList_WhenLoadActiveByRegId_ThenReturnsModel()
    {
        var dto = CreateTestDto();
        _dalMock.Setup(x => x.GetActiveByRegId("RG00001234")).Returns(dto);

        var result = _repository.LoadActiveByRegId("RG00001234");

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.WaitingListId.Should().Be("WTL00000001");
                model.RegId.Should().Be("RG00001234");
                model.IsActive.Should().BeTrue();
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT_RP_06_GivenNoActiveWaitingList_WhenLoadActiveByRegId_ThenReturnsNone()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00009999")).Returns((WaitingListDto)null!);

        var result = _repository.LoadActiveByRegId("RG00009999");

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_07_GivenNoActiveWaitingList_WhenHasActiveByRegId_ThenReturnsFalse()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00009999")).Returns((WaitingListDto)null!);

        var result = _repository.HasActiveByRegId("RG00009999");

        result.Should().BeFalse();
    }

    private static WaitingListModel CreateTestModel() =>
        new(
            "WTL00000001",
            WaitingListStatusEnum.Waiting,
            "RG00001234",
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B001", "Bangsal A"),
            5,
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)));

    private static WaitingListDto CreateTestDto()
    {
        var writeDto = WaitingListDto.FromModel(CreateTestModel());
        return writeDto with
        {
            PasienName = "Pasien Test",
            TglLahir = "1990-05-15",
            Gender = "L"
        };
    }
}
