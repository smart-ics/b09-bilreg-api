using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.WaitingListFeature;

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

        _dalMock.Verify(x => x.Insert(It.Is<WaitingListDto>(d =>
            d.WaitingListId == "WTL26J170001" &&
            d.ArrivalSourceType == (int)ArrivalSourceTypeEnum.Igd &&
            d.ReferringUnitId == "U01" &&
            d.ReferringDoctorId == "DOC01" &&
            d.ClinicalUrgency == (int)ClinicalUrgencyEnum.Emergency &&
            d.PlannedBedId == "BED01" &&
            d.SpecialRequirements == "ISOLASI_AIRBORNE" &&
            d.PreviousWaitListId == "WTL26J160099"
        )), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<WaitingListDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IWaitingListKey>())).Returns(CreateTestDto());

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.Is<WaitingListDto>(d =>
            d.WaitingListId == "WTL26J170001" &&
            d.ArrivalSourceType == (int)ArrivalSourceTypeEnum.Igd &&
            d.ReferringUnitId == "U01" &&
            d.ReferringDoctorId == "DOC01" &&
            d.ClinicalUrgency == (int)ClinicalUrgencyEnum.Emergency &&
            d.SpecialRequirements == "ISOLASI_AIRBORNE"
        )), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<WaitingListDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenNewEntity_WhenSave_ThenInsertIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IWaitingListKey>())).Returns((WaitingListDto)null!);

        _repository.Save(model);

        _dalMock.Verify(x => x.Insert(It.IsAny<WaitingListDto>()), Times.Once);
    }

    [Fact]
    public void UT_RP_04_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto();
        var key = WaitingListModel.Key("WTL26J170001");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.WaitingListId.Should().Be("WTL26J170001");
                model.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
                model.RegId.Should().Be("RG00001234");
                model.Pasien.PasienId.Should().Be("P0001");
                model.Pasien.PasienName.Should().Be("Pasien Test");
                model.Pasien.TglLahir.Should().Be(new DateOnly(1990, 5, 15));
                model.Pasien.Gender.Should().Be("L");
                model.KelasRawat.KelasId.Should().Be("K01");
                model.KelasRawat.KelasName.Should().Be("Kelas 1");
                model.Bangsal.BangsalId.Should().Be("B001");
                model.Bangsal.BangsalName.Should().Be("Bangsal A");
                model.ArrivalSourceType.Should().Be(ArrivalSourceTypeEnum.Igd);
                model.ReferringUnit.LayananId.Should().Be("U01");
                model.ReferringUnit.LayananName.Should().Be("Unit Gawat Darurat");
                model.ReferringDoctor.DokterId.Should().Be("DOC01");
                model.ReferringDoctor.DokterName.Should().Be("dr. Budi Sp.PD");
                model.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
                model.SpecialRequirements.Should().Be("ISOLASI_AIRBORNE");
                model.PlannedBed.BedId.Should().Be("BED01");
                model.PlannedBed.BedName.Should().Be("Bed 01");
                model.PlacedBedId.Should().Be("BED02");
                model.PlacedPakaiBedId.Should().Be("PKB0001");
                model.CancellationReason.Should().Be("Batal karena kondisi");
                model.RerouteReason.Should().Be("Bangsal asal penuh");
                model.PreviousWaitListId.Should().Be("WTL26J160099");
                model.Priority.Should().Be(5);
                model.IsActive.Should().BeTrue();
                model.AuditTrail.Created.UserId.Should().Be("user1");
                model.AuditTrail.Created.Timestamp.Should().Be(new DateTime(2026, 7, 7, 10, 0, 0));
                model.AuditTrail.Modified.UserId.Should().Be("user2");
                model.AuditTrail.Modified.Timestamp.Should().Be(new DateTime(2026, 7, 7, 11, 0, 0));
                model.AuditTrail.Voided.UserId.Should().Be("user3");
                model.AuditTrail.Voided.Timestamp.Should().Be(new DateTime(2026, 7, 7, 12, 0, 0));
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT_RP_05_GivenRegId_WhenHasActiveByRegId_ThenDelegatesToDal()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00001234")).Returns(CreateTestDto());

        var result = _repository.HasActiveByRegId("RG00001234");

        result.Should().BeTrue();
        _dalMock.Verify(x => x.GetActiveByRegId("RG00001234"), Times.Once);
    }

    [Fact]
    public void UT_RP_06_GivenActiveWaitingList_WhenLoadActiveByRegId_ThenReturnsModel()
    {
        var dto = CreateTestDto();
        _dalMock.Setup(x => x.GetActiveByRegId("RG00001234")).Returns(dto);

        var result = _repository.LoadActiveByRegId("RG00001234");

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.WaitingListId.Should().Be("WTL26J170001");
                model.RegId.Should().Be("RG00001234");
                model.IsActive.Should().BeTrue();
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT_RP_07_GivenNoActiveWaitingList_WhenLoadActiveByRegId_ThenReturnsNone()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00009999")).Returns((WaitingListDto)null!);

        var result = _repository.LoadActiveByRegId("RG00009999");

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_08_GivenNoActiveWaitingList_WhenHasActiveByRegId_ThenReturnsFalse()
    {
        _dalMock.Setup(x => x.GetActiveByRegId("RG00009999")).Returns((WaitingListDto)null!);

        var result = _repository.HasActiveByRegId("RG00009999");

        result.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_09_GivenModel_WhenFromModelAndToModel_ThenPreservesAllFields()
    {
        var model = CreateTestModel();

        var dto = WaitingListDto.FromModel(model) with
        {
            PasienName = model.Pasien.PasienName,
            TglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            Gender = model.Pasien.Gender
        };
        var reconstructed = dto.ToModel();

        reconstructed.WaitingListId.Should().Be(model.WaitingListId);
        reconstructed.WaitingListStatus.Should().Be(model.WaitingListStatus);
        reconstructed.RegId.Should().Be(model.RegId);
        reconstructed.Pasien.PasienId.Should().Be(model.Pasien.PasienId);
        reconstructed.Pasien.PasienName.Should().Be(model.Pasien.PasienName);
        reconstructed.Pasien.TglLahir.Should().Be(model.Pasien.TglLahir);
        reconstructed.Pasien.Gender.Should().Be(model.Pasien.Gender);
        reconstructed.KelasRawat.KelasId.Should().Be(model.KelasRawat.KelasId);
        reconstructed.Bangsal.BangsalId.Should().Be(model.Bangsal.BangsalId);
        reconstructed.ArrivalSourceType.Should().Be(model.ArrivalSourceType);
        reconstructed.ReferringUnit.LayananId.Should().Be(model.ReferringUnit.LayananId);
        reconstructed.ReferringDoctor.DokterId.Should().Be(model.ReferringDoctor.DokterId);
        reconstructed.ClinicalUrgency.Should().Be(model.ClinicalUrgency);
        reconstructed.SpecialRequirements.Should().Be(model.SpecialRequirements);
        reconstructed.PlannedBed.BedId.Should().Be(model.PlannedBed.BedId);
        reconstructed.PlacedBedId.Should().Be(model.PlacedBedId);
        reconstructed.PlacedPakaiBedId.Should().Be(model.PlacedPakaiBedId);
        reconstructed.CancellationReason.Should().Be(model.CancellationReason);
        reconstructed.RerouteReason.Should().Be(model.RerouteReason);
        reconstructed.PreviousWaitListId.Should().Be(model.PreviousWaitListId);
        reconstructed.Priority.Should().Be(model.Priority);
    }

    [Fact]
    public void UT_RP_10_GivenRegId_WhenListByRegId_ThenReturnsMappedModels()
    {
        var dto = CreateTestDto();
        _dalMock.Setup(x => x.ListByReg("RG00001234")).Returns(new[] { dto });

        var results = _repository.ListByRegId("RG00001234").ToList();

        results.Should().HaveCount(1);
        results[0].WaitingListId.Should().Be("WTL26J170001");
        results[0].RegId.Should().Be("RG00001234");
    }

    private static WaitingListModel CreateTestModel() =>
        new(
            "WTL26J170001",
            WaitingListStatusEnum.Waiting,
            "RG00001234",
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B001", "Bangsal A"),
            ArrivalSourceTypeEnum.Igd,
            new LayananReff("U01", "Unit Gawat Darurat"),
            new DokterReff("DOC01", "dr. Budi Sp.PD"),
            ClinicalUrgencyEnum.Emergency,
            "ISOLASI_AIRBORNE",
            new BedReff("BED01", "Bed 01", false),
            "Batal karena kondisi",
            "Bangsal asal penuh",
            "WTL26J160099",
            "BED02",
            "PKB0001",
            5,
            new AuditTrailType(
                new AuditInfoType("user1", new DateTime(2026, 7, 7, 10, 0, 0)),
                new AuditInfoType("user2", new DateTime(2026, 7, 7, 11, 0, 0)),
                new AuditInfoType("user3", new DateTime(2026, 7, 7, 12, 0, 0))));

    private static WaitingListDto CreateTestDto()
    {
        var model = CreateTestModel();
        var writeDto = WaitingListDto.FromModel(model);
        return writeDto with
        {
            PasienName = "Pasien Test",
            TglLahir = "1990-05-15",
            Gender = "L"
        };
    }
}
