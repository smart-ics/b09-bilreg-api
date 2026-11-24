using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueEnumsTest
{
    [Fact]
    public void ApotekPhaseEnum_has_expected_values()
    {
        ((int)ApotekPhaseEnum.TAKEN).Should().Be(1);
        ((int)ApotekPhaseEnum.PENERIMAAN).Should().Be(2);
        ((int)ApotekPhaseEnum.PENYIAPAN).Should().Be(3);
        ((int)ApotekPhaseEnum.SIAP_SERAH).Should().Be(4);
        ((int)ApotekPhaseEnum.SERAH_OBAT).Should().Be(5);
        ((int)ApotekPhaseEnum.SELESAI).Should().Be(6);
        ((int)ApotekPhaseEnum.DILEWATI).Should().Be(7);
        ((int)ApotekPhaseEnum.BATAL).Should().Be(8);
        ((int)ApotekPhaseEnum.NO_SHOW).Should().Be(9);
    }

    [Fact]
    public void LoketTypeEnum_has_expected_values()
    {
        ((int)LoketTypeEnum.PenerimaanResep).Should().Be(1);
        ((int)LoketTypeEnum.SerahObat).Should().Be(2);
    }

    [Fact]
    public void LoketClaimStateEnum_has_expected_values()
    {
        ((int)LoketClaimStateEnum.Idle).Should().Be(0);
        ((int)LoketClaimStateEnum.Called).Should().Be(1);
        ((int)LoketClaimStateEnum.InService).Should().Be(2);
    }
}
