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

public class LabTestGroupUseCasesTest
{
    private static readonly DateTime FixedNow = new(2026, 11, 4, 10, 30, 0);
    private readonly Mock<ILabTestGroupRepo> _repoMock = new();
    private readonly ITglJamProvider _clock = new TestTglJamProvider(FixedNow);

    [Fact]
    public async Task ListHandler_ReturnsAllTestGroups()
    {
        var testGroups = new List<LabTestGroupModel>
        {
            LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "u1", FixedNow),
            LabTestGroupModel.Create("GRP-LIP", "Profil Lipid", "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData()).Returns(testGroups);

        var handler = new LabTestGroupListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabTestGroupListQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].TestGroupId.Should().Be("GRP-CBC");
        result[0].TestGroupName.Should().Be("Darah Lengkap");
        result[1].TestGroupId.Should().Be("GRP-LIP");
    }

    [Fact]
    public async Task ListHandler_WithSearch_CallsSearchAndReturnsFiltered()
    {
        var testGroups = new List<LabTestGroupModel>
        {
            LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData("CBC")).Returns(testGroups);

        var handler = new LabTestGroupListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabTestGroupListQuery("CBC"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].TestGroupId.Should().Be("GRP-CBC");
    }

    [Fact]
    public async Task SaveHandler_WhenTestGroupDoesNotExist_CreatesNewAndSaves()
    {
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<ILabTestGroupKey>())).Returns(MayBe<LabTestGroupModel>.None);
        LabTestGroupModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<LabTestGroupModel>()))
            .Callback<LabTestGroupModel>(m => savedModel = m);

        var handler = new LabTestGroupSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabTestGroupSaveCmd("GRP-HISTO", "Histopatologi Jaringan", "admin1");
        var response = await handler.Handle(cmd, CancellationToken.None);

        response.TestGroupId.Should().Be("GRP-HISTO");
        response.TestGroupName.Should().Be("Histopatologi Jaringan");

        savedModel.Should().NotBeNull();
        savedModel!.TestGroupId.Should().Be("GRP-HISTO");
        savedModel.TestGroupName.Should().Be("Histopatologi Jaringan");
        savedModel.AuditTrail.Created.UserId.Should().Be("admin1");
        savedModel.AuditTrail.Created.Timestamp.Should().Be(FixedNow);
    }

    [Fact]
    public async Task SaveHandler_WhenTestGroupExists_UpdatesAndSaves()
    {
        var existing = LabTestGroupModel.Create("GRP-HISTO", "Histopatologi Lama", "creator", new DateTime(2026, 1, 1));
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<ILabTestGroupKey>())).Returns(MayBe.From(existing));
        LabTestGroupModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<LabTestGroupModel>()))
            .Callback<LabTestGroupModel>(m => savedModel = m);

        var handler = new LabTestGroupSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabTestGroupSaveCmd("GRP-HISTO", "Histopatologi Baru", "updater");
        var response = await handler.Handle(cmd, CancellationToken.None);

        response.TestGroupId.Should().Be("GRP-HISTO");
        response.TestGroupName.Should().Be("Histopatologi Baru");

        savedModel.Should().NotBeNull();
        savedModel!.TestGroupId.Should().Be("GRP-HISTO");
        savedModel.TestGroupName.Should().Be("Histopatologi Baru");
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
        var handler = new LabTestGroupSaveHandler(_repoMock.Object, _clock);
        var cmd = new LabTestGroupSaveCmd(id, name, userId);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
