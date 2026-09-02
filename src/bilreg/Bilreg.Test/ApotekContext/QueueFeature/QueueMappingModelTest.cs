using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class QueueMappingModelTest
{
    [Fact]
    public void Create_requires_demandId_antrianId_noUrut_and_mappedBy()
    {
        Action blankDemand = () => QueueMappingModel.Create(
            QueueDemandKindEnum.ResepKerja, "", "Q1", 1, "TRK", QueueMappingMethodEnum.Manual, "u", DateTime.Now);
        blankDemand.Should().Throw<ArgumentException>().WithParameterName("demandId");

        Action blankAntrian = () => QueueMappingModel.Create(
            QueueDemandKindEnum.ResepKerja, "RK1", "", 1, "TRK", QueueMappingMethodEnum.Manual, "u", DateTime.Now);
        blankAntrian.Should().Throw<ArgumentException>().WithParameterName("antrianId");

        Action zeroNoUrut = () => QueueMappingModel.Create(
            QueueDemandKindEnum.ResepKerja, "RK1", "Q1", 0, "TRK", QueueMappingMethodEnum.Manual, "u", DateTime.Now);
        zeroNoUrut.Should().Throw<ArgumentException>().WithParameterName("noUrut");
    }

    [Fact]
    public void Correct_overwrites_queue_identity_and_mapper_metadata()
    {
        var mappedAt = new DateTime(2026, 8, 27, 9, 0, 0);
        var model = QueueMappingModel.Create(
            QueueDemandKindEnum.JualBebas, "ADQ1", "Q-OLD", 1, "TRK-OLD", QueueMappingMethodEnum.Tracker, "mapper-a", mappedAt);

        var correctedAt = mappedAt.AddHours(1);
        model.Correct("Q-NEW", 2, "TRK-NEW", "mapper-b", correctedAt);

        model.AntrianId.Should().Be("Q-NEW");
        model.NoUrut.Should().Be(2);
        model.PasienTrackerId.Should().Be("TRK-NEW");
        model.MappedBy.Should().Be("mapper-b");
        model.MappedAt.Should().Be(correctedAt);
        model.MappingMethod.Should().Be(QueueMappingMethodEnum.Tracker);
    }

    [Fact]
    public void QueueClose_create_requires_reason_staff_and_queue_identity()
    {
        Action blankReason = () => QueueCloseModel.Create("Q1", 1, "", "staff", DateTime.Now);
        blankReason.Should().Throw<ArgumentException>().WithParameterName("reason");

        var close = QueueCloseModel.Create("Q1", 1, "patient left", "staff", DateTime.Now);
        close.QueueCloseId.Should().StartWith(QueueCloseModel.IdPrefix);
        close.AntrianId.Should().Be("Q1");
        close.NoUrut.Should().Be(1);
        close.Reason.Should().Be("patient left");
        close.StaffId.Should().Be("staff");
    }
}
