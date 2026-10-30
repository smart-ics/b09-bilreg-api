using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueDisplayRepoTest
{
    private readonly Mock<IAdmissionQueueDisplayDal> _dalMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly AdmissionQueueDisplayRepo _sut;

    private readonly DateTime _now = new(2026, 10, 29, 8, 30, 0);

    public AdmissionQueueDisplayRepoTest()
    {
        _sut = new AdmissionQueueDisplayRepo(_dalMock.Object, _auditRepoMock.Object);
    }

    [Fact]
    public void LoadEntity_WhenDisplayDoesNotExist_ReturnsNone()
    {
        // Arrange
        _dalMock.Setup(d => d.GetData("DISP-404")).Returns((AdmissionQueueDisplayDto?)null);

        // Act
        var result = _sut.LoadEntity(AdmissionQueueDisplayModel.Key("DISP-404"));

        // Assert
        result.HasValue.Should().BeFalse();
        _dalMock.Verify(d => d.GetData("DISP-404"), Times.Once);
        _dalMock.Verify(d => d.ListLokets(It.IsAny<string>()), Times.Never);
        _dalMock.Verify(d => d.ListPolis(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenDisplayExists_HydratesBothLoketsAndPolis()
    {
        // Arrange
        var displayDto = new AdmissionQueueDisplayDto
        {
            DisplayId = "DISP-CLI",
            DisplayName = "Display Poliklinik Lt 2",
            LocationName = "Lantai 2",
            IsActive = true,
            AudioEnabled = true,
            PollIntervalMs = 3000,
            LayoutKey = "clinic",
            Notes = "Test note",
            RowVersion = 1,
            CrtUser = "admin1",
            CrtDate = _now,
            UpdUser = "admin1",
            UpdDate = _now
        };

        var loketDtos = new List<AdmissionDisplayLoketDto>
        {
            new() { DisplayId = "DISP-CLI", LoketKey = "LK-01", SortOrder = 1 }
        };

        var poliDtos = new List<AdmissionDisplayPoliDto>
        {
            new() { DisplayId = "DISP-CLI", PoliId = "POLI-ANAK", SortOrder = 1 },
            new() { DisplayId = "DISP-CLI", PoliId = "POLI-GIGI", SortOrder = 2 }
        };

        _dalMock.Setup(d => d.GetData("DISP-CLI")).Returns(displayDto);
        _dalMock.Setup(d => d.ListLokets("DISP-CLI")).Returns(loketDtos);
        _dalMock.Setup(d => d.ListPolis("DISP-CLI")).Returns(poliDtos);

        // Act
        var result = _sut.LoadEntity(AdmissionQueueDisplayModel.Key("DISP-CLI"));

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.DisplayId.Should().Be("DISP-CLI");
        model.DisplayName.Should().Be("Display Poliklinik Lt 2");
        model.Active.Should().BeTrue();
        model.LayoutKey.Should().Be("clinic");

        model.Lokets.Should().HaveCount(1);
        model.Lokets[0].LoketKey.Should().Be("LK-01");

        model.Polis.Should().HaveCount(2);
        model.Polis[0].PoliId.Should().Be("POLI-ANAK");
        model.Polis[0].SortOrder.Should().Be(1);
        model.Polis[1].PoliId.Should().Be("POLI-GIGI");
        model.Polis[1].SortOrder.Should().Be(2);

        _dalMock.Verify(d => d.ListPolis("DISP-CLI"), Times.Once);
    }

    [Fact]
    public void ListAll_HydratesPolisGroupedByDisplayId()
    {
        // Arrange
        var displayDtos = new List<AdmissionQueueDisplayDto>
        {
            new()
            {
                DisplayId = "DISP-CLI",
                DisplayName = "Display Poliklinik",
                LocationName = "Lobby B",
                IsActive = true,
                AudioEnabled = true,
                PollIntervalMs = 2000,
                LayoutKey = "clinic",
                Notes = "",
                RowVersion = 1,
                CrtUser = "user1",
                CrtDate = _now,
                UpdUser = "user1",
                UpdDate = _now
            },
            new()
            {
                DisplayId = "DISP-ADM",
                DisplayName = "Display Loket Admisi",
                LocationName = "Lobby A",
                IsActive = true,
                AudioEnabled = true,
                PollIntervalMs = 5000,
                LayoutKey = "default",
                Notes = "",
                RowVersion = 1,
                CrtUser = "user1",
                CrtDate = _now,
                UpdUser = "user1",
                UpdDate = _now
            }
        };

        var allLokets = new List<AdmissionDisplayLoketDto>
        {
            new() { DisplayId = "DISP-ADM", LoketKey = "LK-A", SortOrder = 1 },
            new() { DisplayId = "DISP-ADM", LoketKey = "LK-B", SortOrder = 2 }
        };

        var allPolis = new List<AdmissionDisplayPoliDto>
        {
            new() { DisplayId = "DISP-CLI", PoliId = "POLI-JANTUNG", SortOrder = 1 },
            new() { DisplayId = "DISP-CLI", PoliId = "POLI-PARU", SortOrder = 2 }
        };

        _dalMock.Setup(d => d.ListAll()).Returns(displayDtos);
        _dalMock.Setup(d => d.ListAllLokets()).Returns(allLokets);
        _dalMock.Setup(d => d.ListAllPolis()).Returns(allPolis);

        // Act
        var result = _sut.ListAll();

        // Assert
        result.Should().HaveCount(2);

        var clinicDisplay = result.Single(d => d.DisplayId == "DISP-CLI");
        clinicDisplay.Lokets.Should().BeEmpty();
        clinicDisplay.Polis.Should().HaveCount(2);
        clinicDisplay.Polis.Select(p => p.PoliId).Should().ContainInOrder("POLI-JANTUNG", "POLI-PARU");

        var admDisplay = result.Single(d => d.DisplayId == "DISP-ADM");
        admDisplay.Polis.Should().BeEmpty();
        admDisplay.Lokets.Should().HaveCount(2);
        admDisplay.Lokets.Select(l => l.LoketKey).Should().ContainInOrder("LK-A", "LK-B");

        _dalMock.Verify(d => d.ListAllPolis(), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenExistingDisplayUpdated_LoadsBeforeStateWithPolisForAudit()
    {
        // Arrange
        var existingDto = new AdmissionQueueDisplayDto
        {
            DisplayId = "DISP-CLI",
            DisplayName = "Old Name",
            LocationName = "Old Loc",
            IsActive = true,
            AudioEnabled = true,
            PollIntervalMs = 2000,
            LayoutKey = "clinic",
            Notes = "",
            RowVersion = 1,
            CrtUser = "u1",
            CrtDate = _now,
            UpdUser = "u1",
            UpdDate = _now
        };

        _dalMock.Setup(d => d.GetData("DISP-CLI")).Returns(existingDto);
        _dalMock.Setup(d => d.ListLokets("DISP-CLI")).Returns(new List<AdmissionDisplayLoketDto>());
        _dalMock.Setup(d => d.ListPolis("DISP-CLI")).Returns(new List<AdmissionDisplayPoliDto>
        {
            new() { DisplayId = "DISP-CLI", PoliId = "POLI-A", SortOrder = 1 }
        });
        _dalMock.Setup(d => d.Update(It.IsAny<AdmissionQueueDisplayDto>(), 1, It.IsAny<IReadOnlyList<AdmissionDisplayLoketDto>>()))
            .Returns(1);

        var updatedModel = AdmissionQueueDisplayModel.Load(
            "DISP-CLI", "New Name", "New Loc", true, true, 2000, "clinic", "",
            [],
            rowVersion: 2,
            createdBy: "u1",
            createdAt: _now,
            updatedBy: "u2",
            updatedAt: _now.AddMinutes(5),
            polis: new[] { new AdmissionDisplayPoliMapping("POLI-A", 1) });

        // Act
        _sut.SaveChanges(updatedModel);

        // Assert
        _dalMock.Verify(d => d.ListPolis("DISP-CLI"), Times.Once);
        _auditRepoMock.Verify(a => a.SaveChanges(It.Is<AuditLog>(log =>
            log.EntityId == "DISP-CLI" &&
            log.EntityName == nameof(AdmissionQueueDisplayModel) &&
            log.ActionType == "UPDATE")), Times.Once);
    }
}
