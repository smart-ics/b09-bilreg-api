using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionRepoTest
{
    private readonly Mock<IAdmissionDal> _dalMock = new();
    private readonly AdmissionRepo _repository;

    public AdmissionRepoTest() => _repository = new AdmissionRepo(_dalMock.Object);

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((AdmissionDto)null!);

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.IsAny<AdmissionDto>()), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<AdmissionDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns(CreateTestDto());

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.IsAny<AdmissionDto>()), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<AdmissionDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto();
        var key = AdmissionModel.Key("RG00001234");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.RegId.Should().Be("RG00001234");
                model.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
                model.AdmissionSource.Should().Be(AdmissionSourceEnum.Legacy);
                model.Pasien.PasienId.Should().Be("P0001");
                model.Pasien.PasienName.Should().Be("Pasien Test");
                model.Pasien.TglLahir.Should().Be(new DateOnly(1990, 5, 15));
                model.Pasien.Gender.Should().Be("L");
                model.KelasDk.KelasDkId.Should().Be("1");
                model.Bangsal.BangsalId.Should().Be("B001");
                model.OpnameRequestId.Should().Be("-");
                model.ReservationId.Should().Be("-");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    private static AdmissionModel CreateTestModel() =>
        new(
            "RG00001234",
            AdmissionStatusEnum.Admitted,
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            "-",
            "-",
            new KelasDkType("1", "Kelas DK 1"),
            new BangsalReff("B001", "Bangsal A"),
            new DateTime(2026, 7, 7),
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)),
            AdmissionSourceEnum.Legacy);

    private static AdmissionDto CreateTestDto()
    {
        var writeDto = AdmissionDto.FromModel(CreateTestModel());
        return writeDto with
        {
            PasienName = "Pasien Test",
            TglLahir = "1990-05-15",
            Gender = "L"
        };
    }
}
