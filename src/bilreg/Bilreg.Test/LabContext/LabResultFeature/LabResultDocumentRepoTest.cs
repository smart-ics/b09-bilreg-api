using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultDocumentRepoTest
{
    private readonly Mock<ILabResultDocumentDal> _docDal = new();
    private readonly Mock<ILabResultItemDal> _itemDal = new();
    private readonly LabResultDocumentRepo _sut;

    public LabResultDocumentRepoTest()
    {
        _sut = new LabResultDocumentRepo(_docDal.Object, _itemDal.Object);
    }

    private static LabResultDocumentModel CreateDoc()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO111111111", new AuditInfoType("U1", DateTime.Now));
        var cap = LabResultTestSupport.Capture(numeric: 13m);
        doc.RecordResult(LabResultSourceEnum.Manual, [cap], "U1");
        doc.MarkRecorded("U1");
        return doc;
    }

    [Fact]
    public void SaveChanges_NewEntity_CallsInsertAndReplacesItems()
    {
        var model = CreateDoc();
        _docDal.Setup(x => x.GetData(It.IsAny<ILabResultDocumentKey>())).Returns((LabResultDocumentDto)null!);

        _sut.SaveChanges(model);

        _docDal.Verify(x => x.Insert(It.IsAny<LabResultDocumentDto>()), Times.Once);
        _docDal.Verify(x => x.Update(It.IsAny<LabResultDocumentDto>()), Times.Never);
        _itemDal.Verify(x => x.Delete(model), Times.Once);
        _itemDal.Verify(x => x.Insert(It.Is<IEnumerable<LabResultItemDto>>(l => l.Count() == 1)), Times.Once);
    }

    [Fact]
    public void SaveChanges_ExistingEntity_CallsUpdate()
    {
        var model = CreateDoc();
        _docDal.Setup(x => x.GetData(It.IsAny<ILabResultDocumentKey>())).Returns(LabResultDocumentDto.FromModel(model));

        _sut.SaveChanges(model);

        _docDal.Verify(x => x.Update(It.IsAny<LabResultDocumentDto>()), Times.Once);
        _docDal.Verify(x => x.Insert(It.IsAny<LabResultDocumentDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_AfterVerify_PassesVerifiedStatusAndExpertiseNoteToUpdate()
    {
        var model = CreateDoc();
        model.SetCriticalNote("Nilai kritis trombosit rendah");
        model.Verify("PATH1", new DateTime(2026, 5, 18, 15, 0, 0), "Kesan klinis: Trombositopenia berat");
        _docDal.Setup(x => x.GetData(It.IsAny<ILabResultDocumentKey>())).Returns(LabResultDocumentDto.FromModel(CreateDoc()));

        _sut.SaveChanges(model);

        _docDal.Verify(x => x.Update(It.Is<LabResultDocumentDto>(d =>
            d.ResultStatus == (int)LabResultStatusEnum.Verified
            && d.VerifiedUserId == "PATH1"
            && d.ExpertiseNote == "Kesan klinis: Trombositopenia berat"
            && d.CriticalNote == "Nilai kritis trombosit rendah")), Times.Once);
    }

    [Fact]
    public void SaveChanges_NewEntity_MapsExpertiseAndCriticalNoteToInsertDto()
    {
        var model = CreateDoc();
        model.SetCriticalNote("Catatan kritis segera hubungi DPJP");
        _docDal.Setup(x => x.GetData(It.IsAny<ILabResultDocumentKey>())).Returns((LabResultDocumentDto)null!);

        _sut.SaveChanges(model);

        _docDal.Verify(x => x.Insert(It.Is<LabResultDocumentDto>(d =>
            d.CriticalNote == "Catatan kritis segera hubungi DPJP"
            && d.ExpertiseNote == string.Empty)), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsAggregateWithNotes()
    {
        var model = CreateDoc();
        model.SetCriticalNote("Catatan kritis");
        model.Verify("PATH1", new DateTime(2026, 5, 18, 15, 0, 0), "Ekspertise patolog");
        var dto = LabResultDocumentDto.FromModel(model);
        var itemDto = LabResultItemDto.FromModel(model.ResultDocumentId, model.Items.First());

        _docDal.Setup(x => x.GetData(It.IsAny<ILabResultDocumentKey>())).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<ILabResultDocumentKey>())).Returns([itemDto]);

        var result = _sut.LoadEntity(model);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.Items.Should().HaveCount(1);
                m.ExpertiseNote.Should().Be("Ekspertise patolog");
                m.CriticalNote.Should().Be("Catatan kritis");
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadByOrderId_WhenFound_ReturnsAggregateWithItemsAndNotes()
    {
        var model = CreateDoc();
        model.SetCriticalNote("Catatan kritis");
        model.Verify("PATH1", new DateTime(2026, 5, 18, 15, 0, 0), "Ekspertise patolog");
        var dto = LabResultDocumentDto.FromModel(model);
        var itemDto = LabResultItemDto.FromModel(model.ResultDocumentId, model.Items.First());

        _docDal.Setup(x => x.GetByOrderId("LBO111111111")).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<ILabResultDocumentKey>())).Returns([itemDto]);

        var result = _sut.LoadByOrderId("LBO111111111");

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.Items.Should().HaveCount(1);
                m.OrderId.Should().Be("LBO111111111");
                m.ExpertiseNote.Should().Be("Ekspertise patolog");
                m.CriticalNote.Should().Be("Catatan kritis");
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadByOrderId_WhenMissing_ReturnsNone()
    {
        _docDal.Setup(x => x.GetByOrderId("MISSING")).Returns((LabResultDocumentDto)null!);

        var result = _sut.LoadByOrderId("MISSING");

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void DtoMapping_FromModelAndToModel_PreservesExpertiseAndCriticalNote()
    {
        var model = CreateDoc();
        model.SetCriticalNote("Catatan nilai kritis");
        model.Verify("PATH1", new DateTime(2026, 5, 18, 15, 0, 0), "Catatan ekspertise patolog");

        var dto = LabResultDocumentDto.FromModel(model);

        dto.ExpertiseNote.Should().Be("Catatan ekspertise patolog");
        dto.CriticalNote.Should().Be("Catatan nilai kritis");

        var restored = dto.ToModel(model.Items);

        restored.ExpertiseNote.Should().Be("Catatan ekspertise patolog");
        restored.CriticalNote.Should().Be("Catatan nilai kritis");
        restored.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
        restored.VerifiedUserId.Should().Be("PATH1");
    }
}
