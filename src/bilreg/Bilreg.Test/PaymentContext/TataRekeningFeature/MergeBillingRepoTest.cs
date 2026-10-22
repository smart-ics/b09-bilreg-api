using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

/// <summary>
/// P2-S02: verifies <see cref="MergeBillingRepo"/> persists the merge relation
/// into <c>ta_trs_merge</c> with target = inpatient (fs_kd_reg_merge) and
/// source = preceding registration (fs_kd_reg_asal).
/// </summary>
public class MergeBillingRepoTest
{
    private readonly Mock<IBilrgTrsMergeDal> _dal = new();
    private readonly MergeBillingRepo _sut;

    public MergeBillingRepoTest()
    {
        _sut = new MergeBillingRepo(_dal.Object);
    }

    [Fact]
    public void SaveMergeRelation_WhenNotExisting_ShouldInsertWithCorrectOrientation()
    {
        _dal.Setup(d => d.Exists("REG-RANAP", "REG-IGD")).Returns(false);

        _sut.SaveMergeRelation("REG-RANAP", "REG-IGD");

        _dal.Verify(d => d.Insert(It.Is<BilrgTrsMergeDto>(x =>
            x.MergeRegId == "REG-RANAP" && x.AsalRegId == "REG-IGD")), Times.Once);
    }

    [Fact]
    public void SaveMergeRelation_WhenExisting_ShouldNotDuplicate()
    {
        _dal.Setup(d => d.Exists("REG-RANAP", "REG-IGD")).Returns(true);

        _sut.SaveMergeRelation("REG-RANAP", "REG-IGD");

        _dal.Verify(d => d.Insert(It.IsAny<BilrgTrsMergeDto>()), Times.Never);
    }

    [Fact]
    public void GetTargetRegId_ShouldReturnMergeRegId()
    {
        _dal.Setup(d => d.GetMergeRegId("REG-IGD")).Returns("REG-RANAP");

        _sut.GetTargetRegId("REG-IGD").Should().Be("REG-RANAP");
        _sut.IsMerged("REG-IGD").Should().BeTrue();
    }

    [Fact]
    public void GetMergedSourceRegIds_ShouldReturnAsalIds()
    {
        _dal.Setup(d => d.ListAsalRegId("REG-RANAP")).Returns(["REG-IGD", "REG-POLI"]);

        _sut.GetMergedSourceRegIds("REG-RANAP").Should().BeEquivalentTo(["REG-IGD", "REG-POLI"]);
    }
}
