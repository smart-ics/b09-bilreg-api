using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCreateExternalPatientHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<ILabTestResolutionService> _resolution = new();
    private readonly LabOrderCreateExternalPatientHandler _sut;

    public LabOrderCreateExternalPatientHandlerTest()
    {
        _sequencer.Setup(s => s.GetNextNoUrut("LAB")).Returns(55);
        _resolution
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([new ResolvedLabOrderLine(LabOrderTestSupport.SampleItem(), [])]);
        _sut = new LabOrderCreateExternalPatientHandler(
            _repo.Object,
            _sequencer.Object,
            _resolution.Object,
            TestTglJamProvider.Instance);
    }

    private static LabOrderCreateExternalPatientCmd ValidCmdWithRegExternalId() =>
        new(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: null,
            PatientId: null,
            PatientName: "Budi Pasien Luar",
            BirthDateYmd: "1988-12-05",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")],
            RegExternalId: "LEX26A14B8C1D",
            ExternalMrNo: "EXT-202610-00001");

    private static LabOrderCreateExternalPatientCmd ValidCmdWithRegId() =>
        new(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: "LEX26A14B8C1D",
            PatientId: "EXT-202610-00001",
            PatientName: "Budi Pasien Luar",
            BirthDateYmd: "1988-12-05",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")]);

    [Fact]
    public async Task Handle_WithRegExternalIdAndExternalMrNo_MapsToSnapshotAndSavesOrder()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmdWithRegExternalId(), CancellationToken.None);

        result.OrderId.Should().NotBeNullOrWhiteSpace();
        result.OrderNo.Should().Be("LAB00000055");
        persisted.Should().NotBeNull();
        persisted!.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        persisted.OrderNo.Should().Be("LAB00000055");
        persisted.Patient.RegId.Should().Be("LEX26A14B8C1D");
        persisted.Patient.PatientId.Should().Be("EXT-202610-00001");
        persisted.Patient.PatientName.Should().Be("Budi Pasien Luar");
        persisted.Items.Should().HaveCount(1);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRegIdAndPatientId_MapsToSnapshotAndSavesOrder()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmdWithRegId(), CancellationToken.None);

        result.OrderId.Should().NotBeNullOrWhiteSpace();
        result.OrderNo.Should().Be("LAB00000055");
        persisted.Should().NotBeNull();
        persisted!.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        persisted.Patient.RegId.Should().Be("LEX26A14B8C1D");
        persisted.Patient.PatientId.Should().Be("EXT-202610-00001");
        persisted.Patient.PatientName.Should().Be("Budi Pasien Luar");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { UserId = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyPatientName_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { PatientName = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { Items = [] };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}
