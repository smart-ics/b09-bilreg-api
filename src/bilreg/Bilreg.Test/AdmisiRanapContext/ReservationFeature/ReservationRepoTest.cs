using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.ReservationFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.ReservationFeature;

public class ReservationRepoTest
{
    private readonly Mock<IReservationDal> _dalMock = new();
    private readonly ReservationRepo _repository;

    public ReservationRepoTest() => _repository = new ReservationRepo(_dalMock.Object);

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IReservationKey>())).Returns((ReservationDto)null!);

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.IsAny<ReservationDto>()), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<ReservationDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IReservationKey>())).Returns(CreateTestDto());

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.IsAny<ReservationDto>()), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<ReservationDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto();
        var key = ReservationModel.Key("RSV00000001");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.ReservationId.Should().Be("RSV00000001");
                model.ReservationStatus.Should().Be(ReservationStatusEnum.Reserved);
                model.Pasien.PasienId.Should().Be("P0001");
                model.KelasRawat.KelasId.Should().Be("K01");
                model.Bangsal.BangsalId.Should().Be("B001");
                model.OpnameRequestId.Should().Be("-");
                model.RealizedRegId.Should().Be("-");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    private static ReservationModel CreateTestModel() =>
        new(
            "RSV00000001",
            ReservationStatusEnum.Reserved,
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            "-",
            new DateTime(2026, 7, 10),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B001", "Bangsal A"),
            "-",
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)));

    private static ReservationDto CreateTestDto() => ReservationDto.FromModel(CreateTestModel());
}
