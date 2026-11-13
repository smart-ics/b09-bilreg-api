using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabDisciplineUseCasesTest
{
    private static readonly DateTime FixedNow = new(2026, 11, 4, 10, 30, 0);
    private readonly Mock<ILabDisciplineRepo> _repoMock = new();
    private readonly ITglJamProvider _clock = new TestTglJamProvider(FixedNow);

    [Fact]
    public async Task ListHandler_ReturnsAllDisciplines()
    {
        var disciplines = new List<LabDisciplineModel>
        {
            LabDisciplineModel.Create("DISC-HEM", "Hematologi", "u1", FixedNow),
            LabDisciplineModel.Create("DISC-KIM", "Kimia Klinik", "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData()).Returns(disciplines);

        var handler = new LabDisciplineListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabDisciplineListQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].DisciplineId.Should().Be("DISC-HEM");
        result[0].DisciplineName.Should().Be("Hematologi");
        result[1].DisciplineId.Should().Be("DISC-KIM");
    }

    [Fact]
    public async Task ListHandler_WithSearch_CallsSearchAndReturnsFiltered()
    {
        var disciplines = new List<LabDisciplineModel>
        {
            LabDisciplineModel.Create("DISC-HEM", "Hematologi", "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData("Hem")).Returns(disciplines);

        var handler = new LabDisciplineListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabDisciplineListQuery("Hem"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].DisciplineId.Should().Be("DISC-HEM");
    }

    [Fact]
    public async Task SaveHandler_WhenDisciplineDoesNotExist_CreatesNewAndSaves()
    {
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<ILabDisciplineKey>())).Returns(MayBe<LabDisciplineModel>.None);
        LabDisciplineModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<LabDisciplineModel>()))
            .Callback<LabDisciplineModel>(m => savedModel = m);

        var handler = new LabDisciplineSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabDisciplineSaveCmd("DISC-PAH", "Histopatologi", "admin1");
        var response = await handler.Handle(cmd, CancellationToken.None);

        response.DisciplineId.Should().Be("DISC-PAH");
        response.DisciplineName.Should().Be("Histopatologi");

        savedModel.Should().NotBeNull();
        savedModel!.DisciplineId.Should().Be("DISC-PAH");
        savedModel.DisciplineName.Should().Be("Histopatologi");
        savedModel.AuditTrail.Created.UserId.Should().Be("admin1");
        savedModel.AuditTrail.Created.Timestamp.Should().Be(FixedNow);
    }

    [Fact]
    public async Task SaveHandler_WhenDisciplineExists_UpdatesAndSaves()
    {
        var existing = LabDisciplineModel.Create("DISC-PAH", "Histopatologi Lama", "creator", new DateTime(2026, 1, 1));
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<ILabDisciplineKey>())).Returns(MayBe.From(existing));
        LabDisciplineModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<LabDisciplineModel>()))
            .Callback<LabDisciplineModel>(m => savedModel = m);

        var handler = new LabDisciplineSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabDisciplineSaveCmd("DISC-PAH", "Histopatologi Baru", "updater");
        var response = await handler.Handle(cmd, CancellationToken.None);

        response.DisciplineId.Should().Be("DISC-PAH");
        response.DisciplineName.Should().Be("Histopatologi Baru");

        savedModel.Should().NotBeNull();
        savedModel!.DisciplineId.Should().Be("DISC-PAH");
        savedModel.DisciplineName.Should().Be("Histopatologi Baru");
        savedModel.AuditTrail.Created.UserId.Should().Be("creator");
        savedModel.AuditTrail.Modified.UserId.Should().Be("updater");
        savedModel.AuditTrail.Modified.Timestamp.Should().Be(FixedNow);
    }

    [Theory]
    [InlineData("", "Name", "user")]
    [InlineData("ID", "", "user")]
    [InlineData("ID", "Name", "")]
    public async Task SaveHandler_InvalidInput_ThrowsArgumentException(string id, string name, string userId)
    {
        var handler = new LabDisciplineSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabDisciplineSaveCmd(id, name, userId);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
