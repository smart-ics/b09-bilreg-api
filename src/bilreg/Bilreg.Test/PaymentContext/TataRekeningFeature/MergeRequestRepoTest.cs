using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class MergeRequestRepoTest
{
    private readonly Mock<IBilrgMergeRequestDal> _dalMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly MergeRequestRepo _sut;

    public MergeRequestRepoTest()
    {
        _sut = new MergeRequestRepo(_dalMock.Object, _regRepoMock.Object);
    }

    [Fact]
    public void GivenNewMergeRequest_WhenSaveChanges_ThenInsertsWithPatientId()
    {
        var model = MergeRequestModel.Create("MR-001", "REG-SRC", "REG-TGT");
        _dalMock.Setup(d => d.GetData(It.IsAny<IMergeRequestKey>())).Returns((BilrgMergeRequestDto)null!);
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-SRC")))
            .Returns(MayBe.From(RegModel.Default));

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Insert(It.Is<BilrgMergeRequestDto>(x =>
            x.MergeRequestId == "MR-001")), Times.Once);
        _dalMock.Verify(d => d.Update(It.IsAny<BilrgMergeRequestDto>()), Times.Never);
    }

    [Fact]
    public void GivenExistingPending_WhenExecuteAndSave_ThenUpdatesWithExecutedMetadata()
    {
        var pending = MergeRequestModel.Create("MR-002", "REG-SRC", "REG-TGT");
        var existing = BilrgMergeRequestDto.FromModelForInsert(pending, "PAT-02", "SYSTEM");
        var model = MergeRequestModel.Create("MR-002", "REG-SRC", "REG-TGT");
        model.Execute();
        _dalMock.Setup(d => d.GetData(It.IsAny<IMergeRequestKey>())).Returns(existing);
        _regRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(RegModel.Default));

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Update(It.Is<BilrgMergeRequestDto>(x =>
            x.Status == (int)MergeRequestStatusEnum.Executed &&
            x.ExecutedBy == "SYSTEM")), Times.Once);
    }

    [Fact]
    public void GivenStoredMergeRequest_WhenLoadEntity_ThenReturnsModel()
    {
        var dto = BilrgMergeRequestDto.FromModelForInsert(
            MergeRequestModel.Create("MR-003", "REG-A", "REG-B"), "PAT-03", "SYSTEM");
        _dalMock.Setup(d => d.GetData(It.Is<IMergeRequestKey>(k => k.MergeRequestId == "MR-003")))
            .Returns(dto);

        var result = _sut.LoadEntity(new MergeRequestKey("MR-003"));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.MergeRequestId.Should().Be("MR-003");
                m.Status.Should().Be(MergeRequestStatusEnum.Pending);
            },
            onNone: () => Assert.Fail("Expected merge request"));
    }

    [Fact]
    public void GivenPendingRows_WhenListPendingByReg_ThenReturnsModels()
    {
        var dto = BilrgMergeRequestDto.FromModelForInsert(
            MergeRequestModel.Create("MR-004", "REG-X", "REG-Y"), "PAT-04", "SYSTEM");
        _dalMock.Setup(d => d.ListPendingByReg(It.Is<IRegKey>(k => k.RegId == "REG-X")))
            .Returns([dto]);

        var result = _sut.ListPendingByReg(RegModel.Key("REG-X")).ToList();

        result.Should().HaveCount(1);
        result[0].MergeRequestId.Should().Be("MR-004");
    }
}
