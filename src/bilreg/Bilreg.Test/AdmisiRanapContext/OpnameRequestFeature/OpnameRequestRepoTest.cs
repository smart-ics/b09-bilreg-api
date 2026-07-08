using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class OpnameRequestRepoTest
{
    private readonly Mock<IOpnameRequestDal> _dalMock = new();
    private readonly OpnameRequestRepo _repository;

    public OpnameRequestRepoTest() => _repository = new OpnameRequestRepo(_dalMock.Object);

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IOpnameRequestKey>())).Returns((OpnameRequestDto)null!);

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.IsAny<OpnameRequestDto>()), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<OpnameRequestDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IOpnameRequestKey>())).Returns(CreateTestDto());

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.IsAny<OpnameRequestDto>()), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<OpnameRequestDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto();
        var key = OpnameRequestModel.Key("OPN00000001");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.OpnameRequestId.Should().Be("OPN00000001");
                model.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Requested);
                model.Pasien.PasienId.Should().Be("P0001");
                model.Pasien.PasienName.Should().Be("Pasien Test");
                model.Pasien.TglLahir.Should().Be(new DateOnly(1990, 5, 15));
                model.Pasien.Gender.Should().Be("L");
                model.Dokter.PpaId.Should().Be("D001");
                model.ClinicalNotes.Should().Be("Catatan klinis");
                model.FulfilledRegId.Should().Be("-");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    private static OpnameRequestModel CreateTestModel() =>
        new(
            "OPN00000001",
            OpnameRequestStatusEnum.Requested,
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            new PpaReff("D001", "Dr. Test"),
            "Catatan klinis",
            "-",
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)));

    private static OpnameRequestDto CreateTestDto()
    {
        var writeDto = OpnameRequestDto.FromModel(CreateTestModel());
        return writeDto with
        {
            PasienName = "Pasien Test",
            TglLahir = "1990-05-15",
            Gender = "L"
        };
    }
}
