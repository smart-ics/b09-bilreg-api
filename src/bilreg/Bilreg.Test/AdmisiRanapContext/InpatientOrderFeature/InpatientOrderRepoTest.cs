using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderRepoTest
{
    private readonly Mock<IInpatientOrderDal> _dalMock = new();
    private readonly InpatientOrderRepo _repository;

    public InpatientOrderRepoTest() => _repository = new InpatientOrderRepo(_dalMock.Object);

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        var model = CreateTestModel("OPN26K040001");
        _dalMock.Setup(x => x.GetData(It.IsAny<IInpatientOrderKey>())).Returns((InpatientOrderDto)null!);

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.Is<InpatientOrderDto>(d => d.InpatientOrderId == "OPN26K040001")), Times.Once);
        _dalMock.Verify(x => x.Update(It.IsAny<InpatientOrderDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        var model = CreateTestModel("OPN26K040001");
        _dalMock.Setup(x => x.GetData(It.IsAny<IInpatientOrderKey>())).Returns(CreateTestDto("OPN26K040001"));

        _repository.SaveChanges(model);

        _dalMock.Verify(x => x.Update(It.Is<InpatientOrderDto>(d => d.InpatientOrderId == "OPN26K040001")), Times.Once);
        _dalMock.Verify(x => x.Insert(It.IsAny<InpatientOrderDto>()), Times.Never);
    }

    [Fact]
    public void UT_RP_03_GivenExistingEntity_WhenLoadEntity_ThenFullAggregateIsReconstructed()
    {
        var dto = CreateTestDto("OPN26K040001");
        var key = InpatientOrderModel.Key("OPN26K040001");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.InpatientOrderId.Should().Be("OPN26K040001");
                model.OrderSource.Should().Be(OrderSourceEnum.EmrRawatJalan);
                model.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
                model.Pasien.PasienId.Should().Be("337502200001234");
                model.Pasien.PasienName.Should().Be("SITI AMINAH");
                model.Dokter.PpaId.Should().Be("D001");
                model.KelasRawat.KelasId.Should().Be("1");
                model.Bangsal.BangsalId.Should().Be("B01");
                model.Insurance.TipeJaminan.TipeJaminanId.Should().Be("BPJ");
                model.Insurance.JaminanReffId.Should().Be("0001234567890");
                model.FulfilledRegId.Should().Be("-");
                model.ExternalReferenceId.Should().Be("EXT-ORD-999");
                model.ExternalSystemSource.Should().Be("EMR_RJ");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT_RP_04_GivenNonExistingEntity_WhenLoadEntity_ThenReturnsNone()
    {
        var key = InpatientOrderModel.Key("OPN99999999");
        _dalMock.Setup(x => x.GetData(key)).Returns((InpatientOrderDto)null!);

        var result = _repository.LoadEntity(key);

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_05_GivenKey_WhenGetByKey_ThenReturnsExpectedEntity()
    {
        var dto = CreateTestDto("OPN26K040002");
        var key = InpatientOrderModel.Key("OPN26K040002");
        _dalMock.Setup(x => x.GetData(key)).Returns(dto);

        var result = _repository.GetByKey(key);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.InpatientOrderId.Should().Be("OPN26K040002"),
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void UT_RP_06_GivenStringId_WhenGetByKey_ThenReturnsExpectedEntity()
    {
        var dto = CreateTestDto("OPN26K040003");
        _dalMock.Setup(x => x.GetData(It.Is<IInpatientOrderKey>(k => k.InpatientOrderId == "OPN26K040003"))).Returns(dto);

        var result = _repository.GetByKey("OPN26K040003");

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.InpatientOrderId.Should().Be("OPN26K040003"),
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void UT_RP_07_WhenListActive_ThenDelegatesToDalAndMapsToModel()
    {
        var dtos = new List<InpatientOrderDto>
        {
            CreateTestDto("OPN26K040001"),
            CreateTestDto("OPN26K040002")
        };
        _dalMock.Setup(x => x.ListActive()).Returns(dtos);

        var result = _repository.ListActive().ToList();

        result.Should().HaveCount(2);
        result[0].InpatientOrderId.Should().Be("OPN26K040001");
        result[1].InpatientOrderId.Should().Be("OPN26K040002");
    }

    [Fact]
    public void UT_RP_08_WhenListData_ThenDelegatesToDalAndMapsToModel()
    {
        var filter = new InpatientOrderListFilter(Status: InpatientOrderStatusEnum.Requested);
        var dtos = new List<InpatientOrderDto>
        {
            CreateTestDto("OPN26K040001")
        };
        _dalMock.Setup(x => x.ListData(filter)).Returns(dtos);

        var result = _repository.ListData(filter).ToList();

        result.Should().HaveCount(1);
        result[0].InpatientOrderId.Should().Be("OPN26K040001");
    }

    [Fact]
    public void UT_RP_09_GivenExternalReference_WhenGetByExternalReference_ThenReturnsExpectedEntity()
    {
        var dto = CreateTestDto("OPN26K040001");
        _dalMock.Setup(x => x.GetByExternalReference("EXT-ORD-999", "EMR_RJ")).Returns(dto);

        var result = _repository.GetByExternalReference("EXT-ORD-999", "EMR_RJ");

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.InpatientOrderId.Should().Be("OPN26K040001");
                model.ExternalReferenceId.Should().Be("EXT-ORD-999");
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void UT_RP_10_GivenNonExistentExternalReference_WhenGetByExternalReference_ThenReturnsNone()
    {
        _dalMock.Setup(x => x.GetByExternalReference("UNKNOWN", "EMR_RJ")).Returns((InpatientOrderDto)null!);

        var result = _repository.GetByExternalReference("UNKNOWN", "EMR_RJ");

        result.HasValue.Should().BeFalse();
    }

    private static InpatientOrderModel CreateTestModel(string id) =>
        new(
            id,
            OrderSourceEnum.EmrRawatJalan,
            InpatientOrderStatusEnum.Requested,
            new PasienReff("337502200001234", "SITI AMINAH", new DateOnly(1985, 4, 12), "P"),
            new PpaReff("D001", "dr. Budi Sp.PD"),
            new DateTime(2026, 11, 5, 8, 0, 0),
            new KelasReff("1", "Kelas 1"),
            new BangsalReff("B01", "Bangsal Mawar"),
            "Observasi febris hari ke-4",
            new InpatientOrderInsuranceModel(new TipeJaminanReff("BPJ", "BPJS Kesehatan"), "0001234567890"),
            "-",
            "EXT-ORD-999",
            "EMR_RJ",
            AuditTrailType.Create("dr_budi", new DateTime(2026, 11, 4, 10, 0, 0)));

    private static InpatientOrderDto CreateTestDto(string id) =>
        InpatientOrderDto.FromModel(CreateTestModel(id));
}
