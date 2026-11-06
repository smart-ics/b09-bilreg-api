using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionUseCasesTest
{
    private static readonly DateTime FixedNow = new(2026, 11, 4, 10, 0, 0);
    private readonly ITglJamProvider _clock = new TestTglJamProvider(FixedNow);
    private readonly Mock<ILabTestDefinitionRepo> _definitionRepoMock = new();
    private readonly Mock<ILabComponentMasterRepo> _componentMasterRepoMock = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();

    private void SetupComponentMaster(string id = "MLC0001")
    {
        _componentMasterRepoMock
            .Setup(r => r.LoadEntity(It.Is<ILabComponentMasterKey>(k => k.ComponentId == id)))
            .Returns(MayBe.From(new LabComponentMasterModel(
                id, null, "TEST", "Test Component", LabResultTypeEnum.Numeric, "mg/dL", true, true)));
    }

    [Fact]
    public async Task CreateHandler_PersistsClassificationFields()
    {
        SetupComponentMaster();
        _definitionRepoMock.Setup(r => r.AllocateNextTestDefinitionId()).Returns("LTD0001");
        _definitionRepoMock.Setup(r => r.HasActiveTarifConflict(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        LabTestDefinitionModel? savedModel = null;
        _definitionRepoMock.Setup(r => r.SaveChanges(It.IsAny<LabTestDefinitionModel>()))
            .Callback<LabTestDefinitionModel>(m => savedModel = m);

        var sut = new LabTestDefinitionCreateHandler(
            _definitionRepoMock.Object,
            _componentMasterRepoMock.Object,
            _clock);

        var cmd = new LabTestDefinitionCreateCmd(
            TarifId: "TR1",
            TarifCode: "T01",
            TarifName: "Tarif Biopsi",
            LabTestCode: "PA-BIO",
            LabTestName: "Biopsi",
            SpecimenType: "Jaringan",
            VacutainerType: VacutainerTypeEnum.None,
            IsActive: true,
            UserId: "admin1",
            Components: [new LabTestComponentInput(1, "MLC0001", "", false, true)],
            ServiceType: LaboratoryServiceType.AnatomicalPathology,
            DisciplineId: "DISC-PAH",
            TestGroupId: "GRP-BIO");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.TestDefinitionId.Should().Be("LTD0001");
        savedModel.Should().NotBeNull();
        savedModel!.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        savedModel.DisciplineId.Should().Be("DISC-PAH");
        savedModel.TestGroupId.Should().Be("GRP-BIO");
        savedModel.VacutainerType.Should().Be(VacutainerTypeEnum.None);
    }

    [Fact]
    public async Task UpdateHandler_UpdatesClassificationFields()
    {
        SetupComponentMaster();

        var existing = LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR1", "T01", "Tarif Lama", "OLD", "Tes Lama", "Darah",
            VacutainerTypeEnum.Edta, true, "admin", new DateTime(2026, 1, 1),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            new Dictionary<string, LabComponentMasterModel>
            {
                ["MLC0001"] = new("MLC0001", null, "TEST", "Test", LabResultTypeEnum.Numeric, "", true, true)
            });

        _definitionRepoMock.Setup(r => r.LoadEntity(It.Is<ILabTestDefinitionKey>(k => k.TestDefinitionId == "LTD0001")))
            .Returns(MayBe.From(existing));
        _definitionRepoMock.Setup(r => r.HasActiveTarifConflict(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        LabTestDefinitionModel? savedModel = null;
        _definitionRepoMock.Setup(r => r.SaveChanges(It.IsAny<LabTestDefinitionModel>()))
            .Callback<LabTestDefinitionModel>(m => savedModel = m);

        var sut = new LabTestDefinitionUpdateHandler(
            _definitionRepoMock.Object,
            _componentMasterRepoMock.Object,
            _clock);

        var cmd = new LabTestDefinitionUpdateCmd(
            TestDefinitionId: "LTD0001",
            TarifId: "TR1",
            TarifCode: "T01",
            TarifName: "Tarif Kultur",
            LabTestCode: "MIC-KLT",
            LabTestName: "Kultur Urin",
            SpecimenType: "Urin",
            VacutainerType: VacutainerTypeEnum.None,
            IsActive: true,
            UserId: "updater",
            Components: [new LabTestComponentInput(1, "MLC0001", "", false, true)],
            ServiceType: LaboratoryServiceType.Microbiology,
            DisciplineId: "DISC-MIK",
            TestGroupId: "GRP-MIC");

        await sut.Handle(cmd, CancellationToken.None);

        savedModel.Should().NotBeNull();
        savedModel!.ServiceType.Should().Be(LaboratoryServiceType.Microbiology);
        savedModel.DisciplineId.Should().Be("DISC-MIK");
        savedModel.TestGroupId.Should().Be("GRP-MIC");
        savedModel.VacutainerType.Should().Be(VacutainerTypeEnum.None);
    }

    [Fact]
    public async Task GetHandler_ReturnsClassificationFields()
    {
        var model = LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR1", "T01", "Tarif PA", "PA-BIO", "Biopsi", "Jaringan",
            VacutainerTypeEnum.None, true, "admin", FixedNow,
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            new Dictionary<string, LabComponentMasterModel>
            {
                ["MLC0001"] = new("MLC0001", null, "TEST", "Test", LabResultTypeEnum.Numeric, "", true, true)
            },
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAH",
            testGroupId: "GRP-BIO");

        _definitionRepoMock.Setup(r => r.LoadEntity(It.Is<ILabTestDefinitionKey>(k => k.TestDefinitionId == "LTD0001")))
            .Returns(MayBe.From(model));

        var sut = new LabTestDefinitionGetHandler(_definitionRepoMock.Object, _nilaiTarifRepoMock.Object);
        var result = await sut.Handle(new LabTestDefinitionGetQuery("LTD0001"), CancellationToken.None);

        result.TestDefinitionId.Should().Be("LTD0001");
        result.ServiceType.Should().Be(2);
        result.DisciplineId.Should().Be("DISC-PAH");
        result.TestGroupId.Should().Be("GRP-BIO");
    }

    [Fact]
    public async Task GetByTarifHandler_ReturnsClassificationFields()
    {
        var model = LabTestDefinitionModel.CreateNew(
            "LTD0002", "TR-MIC", "T02", "Tarif Kultur", "MIC-KLT", "Kultur", "Urin",
            VacutainerTypeEnum.None, true, "admin", FixedNow,
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            new Dictionary<string, LabComponentMasterModel>
            {
                ["MLC0001"] = new("MLC0001", null, "TEST", "Test", LabResultTypeEnum.Numeric, "", true, true)
            },
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK",
            testGroupId: "GRP-MIC");

        _definitionRepoMock.Setup(r => r.LoadActiveByTarifId("TR-MIC"))
            .Returns(MayBe.From(model));

        var sut = new LabTestDefinitionGetByTarifHandler(_definitionRepoMock.Object, _nilaiTarifRepoMock.Object);
        var result = await sut.Handle(new LabTestDefinitionGetByTarifQuery("TR-MIC"), CancellationToken.None);

        result.TestDefinitionId.Should().Be("LTD0002");
        result.ServiceType.Should().Be(3);
        result.DisciplineId.Should().Be("DISC-MIK");
        result.TestGroupId.Should().Be("GRP-MIC");
    }
}
